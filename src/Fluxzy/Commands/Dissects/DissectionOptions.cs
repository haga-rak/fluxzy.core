// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionOptions
    {
        public DissectionOptions(
            bool mustBeUnique,
            HashSet<int>? exchangeIds, string format,
            DissectOutputMode outputMode = DissectOutputMode.Format)
        {
            MustBeUnique = mustBeUnique;
            ExchangeIds = exchangeIds;
            Format = format;
            OutputMode = outputMode;
        }

        public string Format { get; }

        public DissectOutputMode OutputMode { get; }

        public bool MustBeUnique { get; }

        public HashSet<int>? ExchangeIds { get; }
    }

    internal enum DissectOutputMode
    {
        Format,
        Json,
        NdJson
    }
}
