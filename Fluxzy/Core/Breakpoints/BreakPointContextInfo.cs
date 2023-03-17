// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointContextInfo
    {
        public BreakPointContextInfo(
            ExchangeInfo exchange,
            bool exchangeComplete,
            BreakPointLocation lastLocation,
            BreakPointLocation? currentHit, Filter originFilter)
        {
            Exchange = exchange;
            LastLocation = lastLocation;
            CurrentHit = currentHit;
            OriginFilter = originFilter;
            Done = LastLocation == BreakPointLocation.WaitingResponse && CurrentHit == null; 
        }

        public int ExchangeId => Exchange.Id; 

        public ExchangeInfo Exchange { get;  }

        public BreakPointLocation LastLocation { get; }

        public BreakPointLocation? CurrentHit { get; }

        public bool Done { get; }

        /// <summary>
        /// The filter that triggers this break point
        /// </summary>
        public Filter OriginFilter { get; }

        public IEnumerable<BreakPointContextStepInfo> StepInfos {
            get
            {
                // enumerate breakpoint locationIndex 

                int index = 1; 

                foreach (BreakPointLocation location in Enum.GetValues(typeof(BreakPointLocation))) {
                    // TODO : minimize the cost of reflection here by using caching 

                    var description = location.GetEnumDescription();

                    if (description == null)
                        continue;

                    var status = CurrentHit == location ? BreakPointStatus.Current
                        : (int) LastLocation >= (int) location ? BreakPointStatus.AlreadyRun
                        : BreakPointStatus.Pending;

                    if (Done) {
                        status = BreakPointStatus.AlreadyRun; 
                    }
                    
                    var stepInfo = new BreakPointContextStepInfo((int) location, $"{index++} - {description}", status);

                    yield return stepInfo;
                }
            }
        }
    }

    public class BreakPointContextStepInfo
    {
        public BreakPointContextStepInfo(int locationIndex, string stepName, BreakPointStatus status)
        {
            LocationIndex = locationIndex;
            StepName = stepName;
            Status = status; 
        }

        public int LocationIndex { get; }

        public string StepName { get; }

        public BreakPointStatus Status { get; }
    }

    public enum BreakPointStatus
    {
        AlreadyRun = 1, 
        Current,
        Pending
    }
}
