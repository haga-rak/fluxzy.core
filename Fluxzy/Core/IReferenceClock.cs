using System;
using System.Diagnostics;

namespace Echoes.Core
{
    public interface IReferenceClock
    {
        DateTime Reference { get;  }

        Stopwatch Watch { get;  }

        DateTime Instant();
    }
}