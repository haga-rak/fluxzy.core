// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Clients
{
    public class BreakPointContext
    {
        private readonly Exchange _exchange;
        private readonly Action<BreakPointContext> _statusChanged;

        public BreakPointContext(Exchange exchange, Action<BreakPointContext> statusChanged)
        {
            _exchange = exchange;
            _statusChanged = statusChanged;

            EndPointCompletion = new BreakPointOrigin<IPEndPoint?>(BreakPointLocation.WaitingEndPoint,
                OnBreakPointStatusUpdate);

            RequestHeaderCompletion = new BreakPointOrigin<Request?>(BreakPointLocation.WaitingRequest,
                OnBreakPointStatusUpdate);

            ResponseHeaderCompletion = new BreakPointOrigin<Response?>(BreakPointLocation.WaitingResponse,
                OnBreakPointStatusUpdate);
        }

        public BreakPointOrigin<Request?> RequestHeaderCompletion { get; set; }

        public BreakPointOrigin<Response?> ResponseHeaderCompletion { get; set; }

        public BreakPointOrigin<IPEndPoint?> EndPointCompletion { get; }

        private void OnBreakPointStatusUpdate(BreakPointLocation? location)
        {
            if (location != null) {
                LastLocation = location.Value;
            }

            CurrentHit = location;

            // Warn parent about context changed 

            _statusChanged(this); 
        }

        public BreakPointLocation LastLocation { get; set; } = BreakPointLocation.Start; 

        public BreakPointLocation ? CurrentHit { get; set; }

        public FilterScope CurrentScope { get; set; }

        public ExchangeInfo ExchangeInfo => new(_exchange); 
        
    }

    public class BreakPointOrigin<T>
    {
        private readonly Action<BreakPointLocation?> _updateReceiver;
        private readonly TaskCompletionSource<T> _waitForValueCompletionSource;

        public BreakPointOrigin(BreakPointLocation location, Action<BreakPointLocation?> updateReceiver)
        {
            _updateReceiver = updateReceiver;
            Location = location;
            _waitForValueCompletionSource = new TaskCompletionSource<T>();
        }

        public BreakPointLocation Location { get;  }

        /// <summary>
        /// null, not runned, true, running, false, finished
        /// </summary>
        public bool ? Running { get; private set; }

        public async Task<T> WaitForValue()
        {
            Running = true; 
            _updateReceiver(Location);

            try
            {
                return await _waitForValueCompletionSource.Task;
            }
            finally
            {
                Running = false;
                _updateReceiver(null);
            }
        }

        public void SetValue(T value)
        {
            _waitForValueCompletionSource.TrySetResult(value);
        }
    }

    public enum BreakPointLocation
    {
        Start = 0,
        WaitingEndPoint, 
        WaitingRequest,
        WaitingResponse,
    }
}
