// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

namespace Fluxzy.Clients
{
    internal static class LoggingConstants
    {
        public static string DefaultTracingDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ".fluxzy", "logs", "debug");
    }
}
