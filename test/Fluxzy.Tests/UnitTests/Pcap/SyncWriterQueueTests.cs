// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Pcap;
using Fluxzy.Tests._Fixtures;
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
        public void GetOrAdd_Then_Rotate_Disposes_PreSubscribe_Writer()
        {
            using var queue = new SyncWriterQueue();

            const long connectionKey = 45678;

            // Packet-arrival path creates a writer before Subscribe.
            var early = queue.GetOrAdd(connectionKey);
            var earlySubId = early.SubscriptionId;

            // Subscribe rotates — must not throw "Already registered!".
            var file = GetRegisteredRandomFile();
            var registered = queue.Rotate(connectionKey);
            registered.Register(file);

            Assert.NotSame(early, registered);
            Assert.NotEqual(earlySubId, registered.SubscriptionId);
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
    }
}
