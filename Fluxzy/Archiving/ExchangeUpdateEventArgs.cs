using System;
using Fluxzy.Clients;
using Fluxzy.Writers;

namespace Fluxzy
{
    public class ExchangeUpdateEventArgs : EventArgs
    {
        public ExchangeInfo ExchangeInfo { get; }

        public Exchange Original { get; }

        public ArchiveUpdateType UpdateType { get; }

        public ExchangeUpdateEventArgs(ExchangeInfo exchangeInfo,
            Exchange original, ArchiveUpdateType updateType)
        {
            ExchangeInfo = exchangeInfo;
            Original = original;
            UpdateType = updateType;
        }
    }
}