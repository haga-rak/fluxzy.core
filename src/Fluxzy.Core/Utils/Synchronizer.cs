// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Synchronizer<T> where T : IEquatable<T>
    {
        private readonly bool _preserve;
        private readonly ConcurrentDictionary<T, LockInfo> _locks = new();

        public static Synchronizer<T> Shared { get; } = new();

        public Synchronizer(bool preserve = false)
        {
            _preserve = preserve;
        }

        public async ValueTask<IDisposable> LockAsync(T key)
        {
            LockInfo lockInfo;

            lock (_locks)
            {
                lockInfo = _locks.GetOrAdd(key, _ => new LockInfo());
                Interlocked.Increment(ref lockInfo.WaitingCount);
            }

            await lockInfo.Semaphore.WaitAsync();

            Interlocked.Increment(ref lockInfo.OwnerCount);
            Interlocked.Decrement(ref lockInfo.WaitingCount);

            return new Releaser(this, key, lockInfo);
        }

        private void Release(T key, LockInfo lockInfo)
        {
            Interlocked.Decrement(ref lockInfo.OwnerCount);
            lockInfo.Semaphore.Release();

            lock (_locks) {
                if (!_preserve
                    && Volatile.Read(ref lockInfo.WaitingCount) == 0
                    && Volatile.Read(ref lockInfo.OwnerCount) == 0)
                {
                    var pair = new KeyValuePair<T, LockInfo>(key, lockInfo);
                    if (((ICollection<KeyValuePair<T, LockInfo>>)_locks).Remove(pair))
                    {
                        lockInfo.Semaphore.Dispose();
                    }
                }
            }
        }

        private class LockInfo
        {
            public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
            public int WaitingCount;
            public int OwnerCount;
        }

        private class Releaser : IDisposable
        {
            private readonly Synchronizer<T> _synchronizer;
            private readonly T _key;
            private readonly LockInfo _lockInfo;
            private bool _disposed;

            public Releaser(Synchronizer<T> synchronizer, T key, LockInfo lockInfo)
            {
                _synchronizer = synchronizer;
                _key = key;
                _lockInfo = lockInfo;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _synchronizer.Release(_key, _lockInfo);
                    _disposed = true;
                }
            }
        }
    }

}
