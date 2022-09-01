using System;
using Fluxzy.Clients;

namespace Fluxzy
{
    public class ExchangeUpdateEventArgs : EventArgs
    {
        public ExchangeInfo ExchangeInfo { get; }
        public Exchange Original { get; }

        public UpdateType UpdateType { get; }

        public ExchangeUpdateEventArgs(ExchangeInfo exchangeInfo,
            Exchange original, UpdateType updateType)
        {
            ExchangeInfo = exchangeInfo;
            Original = original;
            UpdateType = updateType;
        }
    }
}