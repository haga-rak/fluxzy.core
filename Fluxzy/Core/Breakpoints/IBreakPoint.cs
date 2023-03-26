// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Breakpoints
{
    public interface IBreakPoint
    {
        BreakPointLocation Location { get; }

        void Continue();

        bool? Running { get; }
    }
}
