// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using MessagePack;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Net;
using Fluxzy.Misc;
using Xunit;
using Fluxzy.Tests._Fixtures;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class MessagePackQueueExtensionsTests : ProduceDeletableItem
    {
        [Fact]
        public void Test_Serialize_Deserialize()
        {
            var filename = GetRegisteredRandomFile();

            var payload1 = new TestPayload();
            var payload2 = new TestPayload()
            {
                Key = 494949,
                Value = new string('a', 1000),
                Address = IPAddress.Parse("192.168.1.15")
            };

            MessagePackQueueExtensions.AppendMultiple(filename, payload1, GlobalArchiveOption.MessagePackSerializerOptions);
            MessagePackQueueExtensions.AppendMultiple(filename, payload2, GlobalArchiveOption.MessagePackSerializerOptions);
            MessagePackQueueExtensions.AppendMultiple(filename, payload1, GlobalArchiveOption.MessagePackSerializerOptions);
            MessagePackQueueExtensions.AppendMultiple(filename, payload2, GlobalArchiveOption.MessagePackSerializerOptions);

            List<TestPayload> result =
                MessagePackQueueExtensions.DeserializeMultiple<TestPayload>(filename,
                    GlobalArchiveOption.MessagePackSerializerOptions).ToList();


            Assert.Equal(4, result.Count);
            Assert.Equal(payload1, result[0]);
            Assert.Equal(payload2, result[1]);
            Assert.Equal(payload1, result[2]);
            Assert.Equal(payload2, result[3]);
        }

        [Fact]
        public void Test_Serialize_Deserialize_Invalid_End()
        {
            var filename = GetRegisteredRandomFile();

            var payload1 = new TestPayload();
            var payload2 = new TestPayload()
            {
                Key = 494949,
                Value = new string('a', 1000)
            };

            MessagePackQueueExtensions.AppendMultiple(filename, payload1, GlobalArchiveOption.MessagePackSerializerOptions);
            MessagePackQueueExtensions.AppendMultiple(filename, payload2, GlobalArchiveOption.MessagePackSerializerOptions);
            MessagePackQueueExtensions.AppendMultiple(filename, payload1, GlobalArchiveOption.MessagePackSerializerOptions);
            MessagePackQueueExtensions.AppendMultiple(filename, payload2, GlobalArchiveOption.MessagePackSerializerOptions);

            File.AppendAllText(filename, "This ain't mpack");

            List<TestPayload> result =
                MessagePackQueueExtensions.DeserializeMultiple<TestPayload>(filename,
                    GlobalArchiveOption.MessagePackSerializerOptions).ToList();

            Assert.Equal(4, result.Count);
            Assert.Equal(payload1, result[0]);
            Assert.Equal(payload2, result[1]);
            Assert.Equal(payload1, result[2]);
            Assert.Equal(payload2, result[3]);
        }
    }


    [MessagePackObject]
    internal class TestPayload
    {
        protected bool Equals(TestPayload other)
        {
            return Key == other.Key && Value == other.Value && Address.Equals(other.Address);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return Equals((TestPayload) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Value, Address);
        }

        [Key(0)]
        public int Key { get; set; } = 15;

        [Key(1)]
        public string Value { get; set; } = "Cocobelou";

        [Key(3)]
        public IPAddress Address { get; set; } = IPAddress.Loopback;
    }
}
