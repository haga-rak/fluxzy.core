// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    /// <summary>
    /// TS Export modle only 
    /// </summary>
    public class ExchangeState
    {
        public List<ExchangeContainer> Exchanges { get; set; }
        
        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public int TotalCount { get; set; }
    }
}