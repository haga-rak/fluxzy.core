// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class DissectionOptions
    {
        public DissectionOptions(
            bool mustBeUnique,
            HashSet<int>? exchangeIds, string format,
            DissectOutputMode outputMode = DissectOutputMode.Format,
            ExchangeFilterOptions? filter = null,
            bool withHeaders = false,
            int? maxBodyBytes = null)
        {
            WithHeaders = withHeaders;
            MaxBodyBytes = maxBodyBytes;
            MustBeUnique = mustBeUnique;
            ExchangeIds = exchangeIds;
            Format = format;
            OutputMode = outputMode;
            Filter = filter ?? new ExchangeFilterOptions { ExchangeIds = exchangeIds };
        }

        public ExchangeFilterOptions Filter { get; }

        public bool WithHeaders { get; }

        public int? MaxBodyBytes { get; }

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
