// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointContextStepInfo
    {
        public BreakPointContextStepInfo(
            int locationIndex, string stepName, BreakPointStatus status,
            IBreakPointAlterationModel? internalAlterationModel)
        {
            LocationIndex = locationIndex;
            StepName = stepName;
            Status = status;
            InternalAlterationModel = internalAlterationModel;
        }

        public int LocationIndex { get; }

        public string StepName { get; }

        public BreakPointStatus Status { get; }

        [JsonIgnore]
        public IBreakPointAlterationModel? InternalAlterationModel { get; }

        /// <summary>
        /// </summary>
        public object? Model => InternalAlterationModel; // For Sytem.Text.Json Serialization
    }
}
