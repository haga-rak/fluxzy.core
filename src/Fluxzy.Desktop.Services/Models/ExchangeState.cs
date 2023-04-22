// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    /// <summary>
    ///     TS Export modle only
    /// </summary>
    public class ExchangeState
    {
        public List<ExchangeContainer> Exchanges { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public int TotalCount { get; set; }
    }
}
