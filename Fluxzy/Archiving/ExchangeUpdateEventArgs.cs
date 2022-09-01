using System;
using Fluxzy.Clients;

namespace Fluxzy
{
    public class ExchangeUpdateEventArgs : EventArgs
    {
        public ExchangeInfo Exchange { get; }

        public ExchangeUpdateEventArgs(ExchangeInfo exchange)
        {
            Exchange = exchange;
        }
    }
}