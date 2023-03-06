// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients;
using Fluxzy.Writers;

namespace Fluxzy
{
    public class ExchangeUpdateEventArgs : EventArgs
    {
        public ExchangeUpdateEventArgs(
            ExchangeInfo exchangeInfo,
            Exchange original, ArchiveUpdateType updateType)
        {
            ExchangeInfo = exchangeInfo;
            Original = original;
            UpdateType = updateType;
        }

        public ExchangeInfo ExchangeInfo { get; }

        public Exchange Original { get; }

        public ArchiveUpdateType UpdateType { get; }
    }
}
