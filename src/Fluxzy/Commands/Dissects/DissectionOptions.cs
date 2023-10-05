// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionOptions
    {
        public DissectionOptions(
            bool mustBeUnique,
            HashSet<int>?  exchangeIds, string format)
        {
            MustBeUnique = mustBeUnique;
            ExchangeIds = exchangeIds;
            Format = format;
        }

        public string Format { get; }

        public bool MustBeUnique { get;  }

        public HashSet<int>? ExchangeIds { get; }
    }
}
