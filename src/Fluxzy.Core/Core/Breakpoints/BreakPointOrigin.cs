// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointOrigin<T> : IBreakPoint where T : class, IBreakPointAlterationModel, new()
    {
        private readonly Exchange _exchange;
        private readonly Action<IBreakPointAlterationModel, BreakPointLocation, bool> _updateReceiver;
        private readonly TaskCompletionSource<T?> _waitForValueCompletionSource;

        public BreakPointOrigin(
            Exchange exchange,
            BreakPointLocation location, Action<IBreakPointAlterationModel, BreakPointLocation, bool> updateReceiver)
        {
            _exchange = exchange;
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

        public async Task WaitForEdit()
        {
            Running = true;

            var originalValue = new T();

            await originalValue.Init(_exchange).ConfigureAwait(false);

            _updateReceiver(originalValue, Location, false);

            try {
                // We init the value of location 

                var updatedValue = await _waitForValueCompletionSource.Task.ConfigureAwait(false);

                if (updatedValue != null) {
                    originalValue = updatedValue;
                    
                    await updatedValue.Alter(_exchange).ConfigureAwait(false);
                }
                else {
                    // undo 
                    // Set back content body 
                    await originalValue.Alter(_exchange).ConfigureAwait(false);
                }
            }
            finally {
                Running = false;
                _updateReceiver(originalValue, Location, true);
            }
        }

        public void SetValue(T? value)
        {
            _waitForValueCompletionSource.TrySetResult(value);
        }
    }
}
