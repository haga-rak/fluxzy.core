// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;

namespace Fluxzy.Core
{
    internal class ExchangeSourceInitResult 
    {
        public ExchangeSourceInitResult(IDownStreamPipe downStreamPipe, Exchange provisionalExchange)
        {
            DownStreamPipe = downStreamPipe;
            ProvisionalExchange = provisionalExchange;
        }

        public Exchange ProvisionalExchange { get; }

        public IDownStreamPipe DownStreamPipe { get; }
    }
}
