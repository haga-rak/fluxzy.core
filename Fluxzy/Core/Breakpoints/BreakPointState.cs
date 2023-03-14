// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Core.Breakpoints
{
    /// <summary>
    ///     The view model of the breakpoint status
    /// </summary>
    public class BreakPointState
    {
        public BreakPointState(List<BreakPointContextInfo> entries)
        {
            Entries = entries;
        }

        /// <summary>
        ///     Define is debugging window has to popup
        /// </summary>
        public bool HasToPop {
            get { return Entries.Any(e => e.CurrentHit != null); }
        }

        public int ActiveEntries => Entries.Count(e => e.CurrentHit != null); 

        public List<BreakPointContextInfo> Entries { get; }

        public static BreakPointState EmptyEntries { get; } = new(new List<BreakPointContextInfo>());
    }
}
