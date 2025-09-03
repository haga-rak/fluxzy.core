// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MethodHasAsyncOverload

namespace Fluxzy.Clients.H2
{
    internal sealed class WindowSizeHolder : IDisposable
    {
        private readonly H2Logger _logger;

        // Single mutex for BOTH window size and waiters to avoid lost wakeups.
        private readonly object _sync = new();

        // FIFO of async waiters that want "some" window (they re-check on wake).
        private readonly LinkedList<TaskCompletionSource<bool>> _waiters = new();

        // Protected by _sync.
        private int _availableWindowSize;

        public WindowSizeHolder(
            H2Logger logger,
            int availableWindowSize,
            int streamIdentifier)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _availableWindowSize = availableWindowSize;
            StreamIdentifier = streamIdentifier;
            InitialWindowSize = availableWindowSize;
        }

        public int InitialWindowSize { get; private set; }

        public int AvailableWindowSize
        {
            get
            {
                lock (_sync) return _availableWindowSize;
            }
            private set
            {
                lock (_sync) _availableWindowSize = value;
            }
        }

        public int StreamIdentifier { get; }

        public void UpdateInitialWindowSize(int newInitialWindowSize)
        {
            if (newInitialWindowSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newInitialWindowSize));

            int delta = newInitialWindowSize - InitialWindowSize;

            if (delta == 0)
                return; // No change.

            InitialWindowSize = newInitialWindowSize;
            UpdateWindowSize(delta);
        }

        /// <summary>
        /// Adds (or subtracts) to the available window size, saturating at int.MaxValue.
        /// Wakes waiters only when we cross from 0 to &gt; 0 to avoid needless stampedes.
        /// </summary>
        public void UpdateWindowSize(int windowSizeIncrement)
        {
            _logger.Trace(this, windowSizeIncrement);

            List<TaskCompletionSource<bool>>? toRelease = null;

            lock (_sync)
            {
                long before = _availableWindowSize;
                long after = before + (long)windowSizeIncrement;

                if (after > int.MaxValue) 
                    after = int.MaxValue;

                // Window size shouldn't go below 0 for our booking logic; clamp.

                if (after < 0)
                    after = 0;

                _availableWindowSize = (int)after;

                // Only signal when crossing 0 -> >0 and there are waiters.
                if (before <= 0 && after > 0 && _waiters.Count > 0)
                {
                    toRelease = new List<TaskCompletionSource<bool>>(_waiters.Count);
                    foreach (var w in _waiters)
                        toRelease.Add(w);
                    _waiters.Clear();
                }
            }

            // Complete outside lock.
            if (toRelease is not null)
            {
                foreach (var tcs in toRelease)
                    tcs.TrySetResult(true);
            }
        }

        /// <summary>
        /// Attempts to book up to requestedLength bytes of window.
        /// Returns 0 if canceled or if requestedLength == 0.
        /// </summary>
        public async ValueTask<int> BookWindowSize(int requestedLength, CancellationToken cancellationToken)
        {
            if (requestedLength <= 0)
                return 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Fast path under lock: grant immediately if anything is available.
                lock (_sync)
                {
                    var maxAvailable = Math.Min(requestedLength, _availableWindowSize);
                    if (maxAvailable > 0)
                    {
                        _availableWindowSize -= maxAvailable;
                        _logger.Trace(this, -maxAvailable);
                        return maxAvailable;
                    }

                    // If nothing is available, fall through to enqueue a waiter (outside the lock we await).
                }

                // Slow path: enqueue and await notification that window became available.
                TaskCompletionSource<bool> waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
                LinkedListNode<TaskCompletionSource<bool>>? node;

                CancellationTokenRegistration ctr = default;

                lock (_sync)
                {
                    // Re-check under the same lock in case window became available between the two lock sections.
                    var maxAvailable = Math.Min(requestedLength, _availableWindowSize);
                    if (maxAvailable > 0)
                    {
                        _availableWindowSize -= maxAvailable;
                        _logger.Trace(this, -maxAvailable);
                        return maxAvailable;
                    }

                    node = _waiters.AddLast(waiter);

                    if (cancellationToken.CanBeCanceled)
                    {
                        // Capture node for O(1) removal on cancellation.
                        ctr = cancellationToken.Register(static state =>
                        {
                            var (holder, n, t) = ((WindowSizeHolder holder,
                                                   LinkedListNode<TaskCompletionSource<bool>> n,
                                                   TaskCompletionSource<bool> t))state!;

                            lock (holder._sync)
                            {
                                if (n.List is not null)
                                    holder._waiters.Remove(n);
                            }

                            t.TrySetCanceled();
                        }, (this, node, waiter));
                    }
                }

                try
                {
                    await waiter.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    ctr.Dispose();
                    return 0; // mirror prior behavior: treat cancellation as "no bytes booked"
                }
                finally
                {
                    ctr.Dispose();
                }

                // Loop and try to grab bytes now that we were signaled.
            }
        }

        public void Dispose()
        {
            List<TaskCompletionSource<bool>>? toCancel = null;

            lock (_sync)
            {
                if (_waiters.Count > 0)
                {
                    toCancel = new List<TaskCompletionSource<bool>>(_waiters.Count);
                    foreach (var w in _waiters)
                        toCancel.Add(w);
                    _waiters.Clear();
                }
            }

            if (toCancel is not null)
            {
                var ex = new ObjectDisposedException(nameof(WindowSizeHolder));
                foreach (var w in toCancel)
                    w.TrySetException(ex);
            }
        }
    }
}
