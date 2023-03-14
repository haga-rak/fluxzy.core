// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointContext
    {
        private readonly Exchange _exchange;
        private readonly Filter _filter;
        private readonly Action<BreakPointContext> _statusChanged;
        private readonly List<IBreakPoint> _breakPoints = new();

        public BreakPointContext(Exchange exchange,
            Filter filter, 
            Action<BreakPointContext> statusChanged)
        {
            _exchange = exchange;
            _filter = filter;
            _statusChanged = statusChanged;

            EndPointCompletion = new BreakPointOrigin<IPEndPoint?>(BreakPointLocation.WaitingEndPoint,
                OnBreakPointStatusUpdate);

            RequestHeaderCompletion = new BreakPointOrigin<Request?>(BreakPointLocation.WaitingRequest,
                OnBreakPointStatusUpdate);

            ResponseHeaderCompletion = new BreakPointOrigin<Response?>(BreakPointLocation.WaitingResponse,
                OnBreakPointStatusUpdate);

            _breakPoints.Add(EndPointCompletion);
            _breakPoints.Add(RequestHeaderCompletion);
            _breakPoints.Add(ResponseHeaderCompletion);
        }

        public void ContinueUntilEnd()
        {
            foreach (var breakPoint in _breakPoints)
            {
                breakPoint.Continue();
            }
        }

        public void ContinueOnce()
        {
            var breakPoint = _breakPoints.FirstOrDefault(b => b.Running == true); 
            breakPoint?.Continue();
        }

        public BreakPointOrigin<Request?> RequestHeaderCompletion { get; set; }

        public BreakPointOrigin<Response?> ResponseHeaderCompletion { get; set; }

        public BreakPointOrigin<IPEndPoint?> EndPointCompletion { get; }

        private void OnBreakPointStatusUpdate(BreakPointLocation? location)
        {
            if (location != null)
            {
                LastLocation = location.Value;
            }

            CurrentHit = location;

            // Warn parent about context changed 

            _statusChanged(this);
        }

        public BreakPointLocation LastLocation { get; set; } = BreakPointLocation.Start;

        public BreakPointLocation? CurrentHit { get; set; }

        public FilterScope CurrentScope { get; set; }

        public ExchangeInfo ExchangeInfo => new(_exchange);

        public BreakPointContextInfo GetInfo()
        {
            return new(ExchangeInfo, LastLocation, CurrentHit, _filter); 
        }
    }
}
