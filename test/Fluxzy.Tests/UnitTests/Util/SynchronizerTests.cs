// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Utils;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class SynchronizerTests
    {
        [Fact]
        public async Task PreserveMode_RetainsOneEntryPerDistinctKey()
        {
            // Same configuration as PoolBuilder._synchronizer
            var synchronizer = new Synchronizer<Authority>(true);
            const int distinctAuthorityCount = 100;

            for (var i = 0; i < distinctAuthorityCount; i++) {
                var authority = new Authority($"host-{i}.example.com", 443, true);
                (await synchronizer.LockAsync(authority)).Dispose();
            }

            // Nothing is held anymore, yet every LockInfo and its SemaphoreSlim is retained
            Assert.Equal(distinctAuthorityCount, GetRegistryCount(synchronizer));

            // Re-locking existing keys adds nothing: growth tracks distinct-key discovery only
            for (var i = 0; i < distinctAuthorityCount; i++) {
                var authority = new Authority($"host-{i}.example.com", 443, true);
                (await synchronizer.LockAsync(authority)).Dispose();
            }

            Assert.Equal(distinctAuthorityCount, GetRegistryCount(synchronizer));
        }

        [Fact]
        public async Task DefaultMode_RemovesEntryOnceReleased()
        {
            var synchronizer = new Synchronizer<Authority>();
            const int distinctAuthorityCount = 100;

            for (var i = 0; i < distinctAuthorityCount; i++) {
                var authority = new Authority($"host-{i}.example.com", 443, true);
                (await synchronizer.LockAsync(authority)).Dispose();
            }

            Assert.Equal(0, GetRegistryCount(synchronizer));
        }

        [Fact]
        public async Task DefaultMode_KeepsEntryWhileHeldOrAwaited()
        {
            var synchronizer = new Synchronizer<Authority>();
            var authority = new Authority("host.example.com", 443, true);

            var holder = await synchronizer.LockAsync(authority);

            // Registers as waiter synchronously, then awaits the semaphore
            var waiterTask = synchronizer.LockAsync(authority).AsTask();

            Assert.Equal(1, GetRegistryCount(synchronizer));

            holder.Dispose();

            Assert.Equal(1, GetRegistryCount(synchronizer));

            (await waiterTask).Dispose();

            Assert.Equal(0, GetRegistryCount(synchronizer));
        }

        [Fact]
        public async Task DefaultMode_MutualExclusionUnderContention()
        {
            var synchronizer = new Synchronizer<Authority>();
            var authority = new Authority("host.example.com", 443, true);

            var concurrentOwners = 0;
            var overlapDetected = false;

            await Task.WhenAll(Enumerable.Range(0, 32).Select(async _ => {
                for (var i = 0; i < 20; i++) {
                    using var guard = await synchronizer.LockAsync(authority);

                    if (Interlocked.Increment(ref concurrentOwners) > 1)
                        overlapDetected = true;

                    await Task.Yield();

                    Interlocked.Decrement(ref concurrentOwners);
                }
            }));

            Assert.False(overlapDetected);
            Assert.Equal(0, GetRegistryCount(synchronizer));
        }

        [Fact]
        public async Task DefaultMode_HighChurnRemoveRecreateStress()
        {
            // Few keys and no work inside the critical section: entries constantly hit
            // zero owners/waiters and cycle through remove, dispose and recreate
            var synchronizer = new Synchronizer<Authority>();

            var authorities = Enumerable.Range(0, 4)
                                        .Select(i => new Authority($"host-{i}.example.com", 443, true))
                                        .ToArray();

            var owners = new int[authorities.Length];
            var overlapDetected = false;

            await Task.WhenAll(Enumerable.Range(0, 16).Select(worker => Task.Run(async () => {
                for (var i = 0; i < 2500; i++) {
                    var index = (worker + i) % authorities.Length;

                    using var guard = await synchronizer.LockAsync(authorities[index]);

                    if (Interlocked.Increment(ref owners[index]) > 1)
                        overlapDetected = true;

                    Interlocked.Decrement(ref owners[index]);
                }
            })));

            Assert.False(overlapDetected);
            Assert.Equal(0, GetRegistryCount(synchronizer));
        }

        [Fact]
        public void PoolBuilder_UsesSelfCleaningSynchronizer()
        {
            // Guards against reintroducing the unbounded per-authority lock registry
            var poolBuilder = new PoolBuilder(null!, null!, null!, null!);

            var synchronizer = typeof(PoolBuilder)
                .GetField("_synchronizer", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(poolBuilder)!;

            var preserve = (bool) synchronizer.GetType()
                .GetField("_preserve", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(synchronizer)!;

            Assert.False(preserve);
        }

        private static int GetRegistryCount<T>(Synchronizer<T> synchronizer) where T : IEquatable<T>
        {
            var locksField = typeof(Synchronizer<T>)
                .GetField("_locks", BindingFlags.NonPublic | BindingFlags.Instance)!;

            return ((ICollection) locksField.GetValue(synchronizer)!).Count;
        }
    }
}
