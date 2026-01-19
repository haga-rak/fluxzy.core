// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Cli.Commands.PrettyOutput
{
    /// <summary>
    /// Represents a single exchange row for display in the pretty output table.
    /// </summary>
    public class ExchangeDisplayEntry
    {
        public int Id { get; init; }

        public DateTime Timestamp { get; init; }

        public string Method { get; init; } = string.Empty;

        public string Host { get; init; } = string.Empty;

        public string Path { get; init; } = string.Empty;

        public int StatusCode { get; init; }

        public long Size { get; init; }

        public double ResponseTimeMs { get; init; }

        public bool HasError { get; init; }
    }
}
