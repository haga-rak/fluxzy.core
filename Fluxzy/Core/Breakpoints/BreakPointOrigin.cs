// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointOrigin<T> : IBreakPoint
    {
        private readonly Action<BreakPointLocation?> _updateReceiver;
        private readonly TaskCompletionSource<T?> _waitForValueCompletionSource;

        public BreakPointOrigin(BreakPointLocation location, Action<BreakPointLocation?> updateReceiver)
        {
            _updateReceiver = updateReceiver;
            Location = location;
            _waitForValueCompletionSource = new TaskCompletionSource<T?>();
        }

        public BreakPointLocation Location { get; }

        /// <summary>
        ///     null, not run, true, running, false, finished
        /// </summary>
        public bool? Running { get; private set; }

        public void Continue()
        {
            SetValue(default);
        }

        public async Task<T?> WaitForValue()
        {
            Running = true;
            _updateReceiver(Location);

            try {
                return await _waitForValueCompletionSource.Task;
            }
            finally {
                Running = false;
                _updateReceiver(null);
            }
        }

        public void SetValue(T? value)
        {
            _waitForValueCompletionSource.TrySetResult(value);
        }
    }
}
