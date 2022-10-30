// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public class ArchiveMetaInformation
    {
        public DateTime CaptureDate { get; set; } = DateTime.Now;

        public HashSet<Tag> Tags { get; set; } = new();

        public List<Filter> ViewFilters { get; set; } = new(); 
    }
}