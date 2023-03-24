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
        private readonly Action<IBreakPointAlterationModel, BreakPointContext> _statusChanged;
        private readonly Dictionary<BreakPointLocation, IBreakPointAlterationModel> _alterationModels =
            new();

        internal List<IBreakPoint> BreakPoints { get; } = new();

        private bool _previousStatus;

        public BreakPointContext(
            Exchange exchange, Filter filter,
            Action<IBreakPointAlterationModel, BreakPointContext> statusChanged)
        {
            _exchange = exchange;
            _filter = filter;
            _statusChanged = statusChanged;

            ConnectionSetupCompletion =
                new BreakPointOrigin<ConnectionSetupStepModel>(exchange, BreakPointLocation.PreparingRequest,
                    OnBreakPointStatusUpdate);

            RequestHeaderCompletion = new BreakPointOrigin<RequestSetupStepModel>(exchange,
                BreakPointLocation.Request,
                OnBreakPointStatusUpdate);

            ResponseHeaderCompletion = new BreakPointOrigin<ResponseSetupStepModel>(exchange,
                BreakPointLocation.Response,
                OnBreakPointStatusUpdate);

            BreakPoints.Add(ConnectionSetupCompletion);
            BreakPoints.Add(RequestHeaderCompletion);
            BreakPoints.Add(ResponseHeaderCompletion);
        }

        public void ContinueUntilEnd()
        {
            foreach (var breakPoint in BreakPoints) {
                breakPoint.Continue();
            }
        }
        public void ContinueUntil(BreakPointLocation location)
        {
            foreach (var breakPoint in BreakPoints) {
                if (breakPoint.Location == location)
                    return; 

                breakPoint.Continue();
            }
        }

        public void ContinueOnce()
        {
            var breakPoint = BreakPoints.FirstOrDefault(b => b.Running == true);
            breakPoint?.Continue();
        }

        public BreakPointOrigin<ConnectionSetupStepModel> ConnectionSetupCompletion { get; set; }

        public BreakPointOrigin<RequestSetupStepModel> RequestHeaderCompletion { get; set; }

        public BreakPointOrigin<ResponseSetupStepModel> ResponseHeaderCompletion { get; }

        private void OnBreakPointStatusUpdate(IBreakPointAlterationModel alterationModel,
            BreakPointLocation definitiveLocation, bool done)
        {
            if (!done)
                LastLocation = definitiveLocation;

            CurrentHit = done ? null : definitiveLocation;
                
            _alterationModels[definitiveLocation] = alterationModel; // had to update correctly here 

            // Warn parent about context changed 

            _statusChanged(alterationModel, this);
        }

        public BreakPointLocation LastLocation { get; set; } = BreakPointLocation.Start;

        public BreakPointLocation? CurrentHit { get; set; }

        public FilterScope CurrentScope { get; set; }

        public ExchangeInfo ExchangeInfo => new(_exchange);

        public BreakPointContextInfo GetInfo()
        {
            _previousStatus = _previousStatus || _exchange.Complete.IsCompleted ||
                              _exchange.Complete.Status >= TaskStatus.RanToCompletion;

            return new BreakPointContextInfo(_alterationModels,
                ExchangeInfo, _previousStatus, LastLocation, CurrentHit, _filter);
        }
    }

    /// <summary>
    ///     Définit une modèle d'aleration d'un econnection
    /// </summary>
    public interface IBreakPointAlterationModel : IValidatableObject
    {
        ValueTask Init(Exchange exchange);

        ValueTask Alter(Exchange exchange);

        bool Done { get; }
    }
}
