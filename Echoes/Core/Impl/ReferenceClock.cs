using System;
using System.Diagnostics;

namespace Echoes.Core
{
    public class ReferenceClock : IReferenceClock
    {
        public ReferenceClock()
        {
            Watch = new Stopwatch();
            Reference = DateTime.Now;
            Watch.Start();
            Reference = DateTime.SpecifyKind(Reference, DateTimeKind.Local);
        }

        public DateTime Reference { get;  }

        public Stopwatch Watch { get;  }

        public DateTime Instant()
        {
            //return DateTime.Now;
            return Reference.Add(Watch.Elapsed);
        }
    }
}