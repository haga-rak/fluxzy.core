// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Fluxzy.Utils
{
    /// <summary>
    ///  A lock by value accepting an IEquatable type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class Synchronizer<T> where T : IEquatable<T>
    {
        private readonly bool _preserve;
        private readonly ConcurrentDictionary<T, SemaphoreSlim> _locks = new();

        public static Synchronizer<T> Shared { get; } = new();

        public Synchronizer(bool preserve = false)
        {
            _preserve = preserve;
        }

        public async ValueTask<IDisposable> LockAsync(T key)
        {
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            if (!semaphore.Wait(0))
                await semaphore.WaitAsync();

            return new Releaser(this, key);
        }

        private void Release(T key)
        {
            if (_locks.TryGetValue(key, out var semaphore))
            {
                semaphore.Release();

                // Clean up if no one is waiting
                if (!_preserve && semaphore.CurrentCount == 1)
                {
                    _locks.TryRemove(key, out _);
                }
            }
        }

        private class Releaser : IDisposable
        {
            private readonly Synchronizer<T> _valueLock;
            private readonly T _key;
            private bool _disposed;

            public Releaser(Synchronizer<T> valueLock, T key)
            {
                _valueLock = valueLock;
                _key = key;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _valueLock.Release(_key);
                    _disposed = true;
                }
            }
        }
    }
}
