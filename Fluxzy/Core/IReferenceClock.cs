using System;
using System.Diagnostics;

namespace Fluxzy.Core
{
    public interface IReferenceClock
    {
        DateTime Reference { get;  }

        Stopwatch Watch { get;  }

        DateTime Instant();
    }
}