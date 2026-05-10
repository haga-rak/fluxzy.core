// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;

namespace Fluxzy.Cli.Commands
{
    public interface IFluxzyConsole
    {
        IConsoleWriter Out { get; }

        IConsoleWriter Error { get; }

        Stream BinaryStdout { get; }

        Stream BinaryStderr { get; }

        string? StandardInputContent { get; }
    }
}
