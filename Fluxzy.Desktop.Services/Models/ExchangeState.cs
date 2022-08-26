// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class ExchangeState
    {
        public List<ExchangeInfo> Exchanges { get; set; }

        public int Count { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public int TotalCount { get; set; }

        public static ExchangeState Empty()
        {
            return new ExchangeState()
            {
                Exchanges = new()
            };
        }
    }
}