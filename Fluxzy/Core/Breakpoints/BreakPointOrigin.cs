// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointOrigin<T> : IBreakPoint where T : class, IBreakPointAlterationModel, new()
    {
        private readonly Exchange _exchange;
        private readonly Action<BreakPointLocation?> _updateReceiver;
        private readonly TaskCompletionSource<bool> _waitForValueCompletionSource;

        public BreakPointOrigin(Exchange exchange,
            BreakPointLocation location, Action<BreakPointLocation?> updateReceiver)
        {
            _exchange = exchange;
            _updateReceiver = updateReceiver;
            Value = new T();
            Location = location;
            _waitForValueCompletionSource = new TaskCompletionSource<bool>();
        }
        
        public T Value { get; private set; }

        
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
            
            await Value.Init(_exchange);
            
            _updateReceiver(Location);

            try {
                // We init the value of location 
                
                var res = await _waitForValueCompletionSource.Task;

                if (res) {
                    Value.Alter(_exchange);
                }

            }
            finally {
                Running = false;
                _updateReceiver(null);
            }
        }

        public void SetValue(T? value)
        {
            if (value != null) {
                Value = value;
            }
            
            _waitForValueCompletionSource.TrySetResult(value != null);
        }
    }
}
