// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;

namespace Fluxzy.Core.Breakpoints
{
    public class BreakPointContextStepInfo
    {
        public BreakPointContextStepInfo(
            BreakPointLocation location, string stepName, BreakPointStatus status,
            IBreakPointAlterationModel? internalAlterationModel)
        {
            LocationIndex = (int) location;
            Location = location;
            StepName = stepName;
            Status = status;
            InternalAlterationModel = internalAlterationModel;
        }

        public int LocationIndex { get; }

        public BreakPointLocation Location { get; }

        public string StepName { get; }

        public BreakPointStatus Status { get; }

        [JsonIgnore]
        public IBreakPointAlterationModel? InternalAlterationModel { get; }

        /// <summary>
        /// </summary>
        public object? Model => InternalAlterationModel; // For Sytem.Text.Json Serialization
    }
}
