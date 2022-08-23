// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    /// <summary>
    /// Count must be set.
    /// When endIndex is null, read should be from the end
    /// When startIndex is null, read should be from the start 
    /// </summary>
    public class ExchangeBrowsingState
    {
        public int? StartIndex { get; set; }

        public int? EndIndex { get; set; }

        public int Count { get; set; }
    }
}