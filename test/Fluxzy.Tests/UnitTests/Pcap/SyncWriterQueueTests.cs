// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Pcapng.Structs;
using Fluxzy.Tests._Fixtures;
using NSubstitute;
using PacketDotNet;
using SharpPcap;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap
{
    public class SyncWriterQueueTests : ProduceDeletableItem
    {
        [Fact]
        public void Rotate_Replaces_Stale_Writer_On_Same_Key()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 12345;

            var firstFile = GetRegisteredRandomFile();
            var first = queue.Rotate(connectionKey);
            first.Register(firstFile);

            var firstSubId = first.SubscriptionId;

            var secondFile = GetRegisteredRandomFile();
            var second = queue.Rotate(connectionKey);
            second.Register(secondFile);

            Assert.NotSame(first, second);
            Assert.NotEqual(firstSubId, second.SubscriptionId);
            Assert.Equal(connectionKey, second.Key);
        }

        [Fact]
        public void Stale_Unsubscribe_Is_NoOp_After_Rotate()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 23456;

            var firstFile = GetRegisteredRandomFile();
            var first = queue.Rotate(connectionKey);
            first.Register(firstFile);
            var firstSubId = first.SubscriptionId;

            var secondFile = GetRegisteredRandomFile();
            var second = queue.Rotate(connectionKey);
            second.Register(secondFile);

            // Simulate the old connection's linger-delayed Unsubscribe firing after rotation.
            var removed = queue.TryRemoveBySubId(firstSubId, out _);
            Assert.False(removed);

            Assert.True(queue.TryGet(connectionKey, out var current));
            Assert.Same(second, current);
        }

        [Fact]
        public void Unsubscribe_Removes_Current_Writer()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 34567;

            var file = GetRegisteredRandomFile();
            var writer = queue.Rotate(connectionKey);
            writer.Register(file);

            Assert.True(queue.TryRemoveBySubId(writer.SubscriptionId, out _));
            Assert.False(queue.TryGet(connectionKey, out _));
        }

        [Fact]
        public void GetOrAdd_Then_Rotate_Keeps_PreSubscribe_Writer()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 45678;

            var early = queue.GetOrAdd(connectionKey);

            var file = GetRegisteredRandomFile();
            var registered = queue.Rotate(connectionKey);
            registered.Register(file);

            Assert.Same(early, registered);
            Assert.True(registered.Registered);
        }

        [Fact]
        public void Double_Register_On_Same_Connection_Key_Does_Not_Throw()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 56789;

            var firstFile = GetRegisteredRandomFile();
            var first = queue.Rotate(connectionKey);
            first.Register(firstFile);

            var secondFile = GetRegisteredRandomFile();
            var second = queue.Rotate(connectionKey);

            // Regression for "InvalidOperationException: Already registered!"
            var ex = Record.Exception(() => second.Register(secondFile));
            Assert.Null(ex);
        }

        [Fact]
        public void Packets_Buffered_Before_Register_Are_Written_To_File()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 67890;

            var early = queue.GetOrAdd(connectionKey);
            var payloads = new[] { new byte[60], new byte[74], new byte[66] };

            for (var i = 0; i < payloads.Length; i++) {
                payloads[i].AsSpan().Fill((byte) (i + 1));
                WritePacket(early, payloads[i]);
            }

            var file = GetRegisteredRandomFile();
            var registered = queue.Rotate(connectionKey);
            registered.Register(file);

            var late = new byte[100];
            late.AsSpan().Fill(9);
            WritePacket(registered, late);

            Assert.True(queue.TryRemoveBySubId(registered.SubscriptionId, out _));

            var blocks = ReadEnhancedPacketLengths(file);

            Assert.Equal(new[] { 60, 74, 66, 100 }, blocks);
        }

        [Fact]
        public void Rotate_Keeps_Pending_Writer_After_Port_Reuse_Rotation()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 78901;

            var lingering = queue.Rotate(connectionKey);
            lingering.Register(GetRegisteredRandomFile());
            var lingeringSubId = lingering.SubscriptionId;

            var early = queue.GetOrAdd(connectionKey);
            Assert.Same(lingering, early);

            var fresh = queue.Rotate(connectionKey);
            Assert.NotSame(lingering, fresh);
            Assert.False(fresh.Registered);

            var pending = queue.GetOrAdd(connectionKey);
            Assert.Same(fresh, pending);

            var reused = queue.Rotate(connectionKey);
            Assert.Same(fresh, reused);

            reused.Register(GetRegisteredRandomFile());

            Assert.False(queue.TryRemoveBySubId(lingeringSubId, out _));
            Assert.True(queue.TryGet(connectionKey, out var current));
            Assert.Same(fresh, current);
        }

        private static void WritePacket(Fluxzy.Core.Pcap.Writing.IRawCaptureWriter writer, byte[] data)
        {
            var device = Substitute.For<ICaptureDevice>();
            device.MacAddress.Returns(PhysicalAddress.Parse("00-11-22-33-44-55"));
            device.LinkType.Returns(LinkLayers.Ethernet);
            device.Name.Returns("fake0");
            device.Description.Returns("fake device");

            var header = Substitute.For<ICaptureHeader>();
            header.Timeval.Returns(new PosixTimeval(DateTime.UtcNow));

            var capture = new PacketCapture(device, header, data);

            writer.Write(ref capture);
        }

        private static List<int> ReadEnhancedPacketLengths(string file)
        {
            var bytes = File.ReadAllBytes(file);
            var result = new List<int>();
            var offset = 0;

            while (offset + 8 <= bytes.Length) {
                var blockType = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
                var blockLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 4));

                if (blockType == EnhancedPacketBlock.BlockTypeValue) {
                    result.Add(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 20)));
                }

                offset += blockLength;
            }

            return result;
        }
    }
}
