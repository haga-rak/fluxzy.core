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

        // Mutex for the waiter queue only. The value field uses lock-free CAS.
        private readonly object _sync = new();

        // FIFO of async waiters that want "some" window (they re-check on wake).
        private readonly LinkedList<TaskCompletionSource<bool>> _waiters = new();

        // Updated via Interlocked.CompareExchange — no monitor lock on the fast path.
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

        public int AvailableWindowSize => Volatile.Read(ref _availableWindowSize);

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
        /// Uses lock-free CAS for the value; acquires lock only to signal ONE waiter.
        /// Wakes a single waiter when we cross from 0 to &gt; 0; that waiter cascades
        /// to the next if window remains after booking, avoiding thundering herd.
        /// </summary>
        public void UpdateWindowSize(int windowSizeIncrement)
        {
            _logger.Trace(this, windowSizeIncrement);

            // Lock-free CAS update of the value.
            int before, after;
            while (true)
            {
                before = Volatile.Read(ref _availableWindowSize);
                long afterLong = (long)before + windowSizeIncrement;

                if (afterLong > int.MaxValue)
                    afterLong = int.MaxValue;

                if (afterLong < 0)
                    afterLong = 0;

                after = (int)afterLong;

                if (Interlocked.CompareExchange(ref _availableWindowSize, after, before) == before)
                    break;
            }

            // Signal ONE waiter whenever window is positive — not just on the
            // 0→positive transition.  When two concurrent UpdateWindowSize calls
            // race, only one observes before≤0; the other sees before>0 and skips
            // the wake even though a waiter may still be queued.  WakeOneWaiter is
            // cheap (lock + empty-check) when no waiters exist.
            if (after > 0)
            {
                WakeOneWaiter();
            }
        }

        /// <summary>
        /// Dequeues and completes the first waiter in FIFO order.
        /// Called when window becomes available so exactly one waiter wakes up.
        /// After that waiter books its share, it cascades to the next if window remains.
        /// </summary>
        private void WakeOneWaiter()
        {
            TaskCompletionSource<bool>? toRelease = null;

            lock (_sync)
            {
                if (_waiters.Count > 0)
                {
                    toRelease = _waiters.First!.Value;
                    _waiters.RemoveFirst();
                }
            }

            // Complete outside lock to avoid holding lock during continuation.
            toRelease?.TrySetResult(true);
        }

        /// <summary>
        /// Attempts to book up to requestedLength bytes of window.
        /// Returns 0 if canceled or if requestedLength == 0.
        /// Fast path is lock-free (CAS); slow path uses lock for waiter queue only.
        /// After a successful booking, cascades a wake to the next waiter if
        /// window remains, ensuring fair progress without thundering herd.
        /// </summary>
        public async ValueTask<int> BookWindowSize(int requestedLength, CancellationToken cancellationToken)
        {
            if (requestedLength <= 0)
                return 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Lock-free fast path: CAS to atomically book available window.
                {
                    var current = Volatile.Read(ref _availableWindowSize);
                    if (current > 0)
                    {
                        var grant = Math.Min(requestedLength, current);
                        if (Interlocked.CompareExchange(ref _availableWindowSize, current - grant, current) == current)
                        {
                            _logger.Trace(this, -grant);

                            // Cascade: if window remains after our booking, wake the next waiter.
                            if (current - grant > 0)
                                WakeOneWaiter();

                            return grant;
                        }
                        continue; // CAS failed (concurrent modification), retry fast path.
                    }
                }

                // Slow path: enqueue waiter and await notification that window became available.
                CancellationTokenRegistration ctr = default;
                TaskCompletionSource<bool> waiter;

                lock (_sync)
                {
                    // Re-check under lock — must be atomic with waiter addition to prevent lost wakeups.
                    // Use CAS for the value to stay compatible with lock-free UpdateWindowSize.
                    var current = Volatile.Read(ref _availableWindowSize);
                    if (current > 0)
                    {
                        var grant = Math.Min(requestedLength, current);
                        if (Interlocked.CompareExchange(ref _availableWindowSize, current - grant, current) == current)
                        {
                            _logger.Trace(this, -grant);

                            if (current - grant > 0)
                                WakeOneWaiter();

                            return grant;
                        }
                        // CAS failed — another thread modified the value concurrently.
                        // Exit lock and retry the outer loop (fast path will pick it up).
                        continue;
                    }

                    // Window is exhausted — create and enqueue waiter.
                    waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var node = _waiters.AddLast(waiter);

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
