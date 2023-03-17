// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
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
        private bool _previousStatus = false; 

        public BreakPointContext(Exchange exchange,  Filter filter, 
            Action<BreakPointContext> statusChanged)
        {
            _exchange = exchange;
            _filter = filter;
            _statusChanged = statusChanged;

            ConnectionSetupCompletion = 
                new BreakPointOrigin<ConnectionSetupStepModel>(exchange, BreakPointLocation.WaitingEndPoint,
                OnBreakPointStatusUpdate);

            RequestHeaderCompletion = new BreakPointOrigin<RequestSetupStepModel>(exchange, BreakPointLocation.WaitingRequest,
                OnBreakPointStatusUpdate);

            ResponseHeaderCompletion = new BreakPointOrigin<ResponseSetupStepModel>(exchange, BreakPointLocation.WaitingResponse,
                OnBreakPointStatusUpdate);

            _breakPoints.Add(ConnectionSetupCompletion);
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

        public BreakPointOrigin<ConnectionSetupStepModel> ConnectionSetupCompletion { get; set; }

        public BreakPointOrigin<RequestSetupStepModel> RequestHeaderCompletion { get; set; }

        public BreakPointOrigin<ResponseSetupStepModel> ResponseHeaderCompletion { get; }

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
            _previousStatus = (_previousStatus) || (_exchange.Complete.IsCompleted || _exchange.Complete.Status >= TaskStatus.RanToCompletion);
            return new(ExchangeInfo, _previousStatus, LastLocation, CurrentHit, _filter); 
        }
    }

    /// <summary>
    /// Définit une modèle d'aleration d'un econnection 
    /// </summary>
    public interface IBreakPointAlterationModel : IValidatableObject
    {
        ValueTask Init(Exchange exchange); 
        
        void Alter(Exchange exchange);

        bool Done { get; }
    }
}
