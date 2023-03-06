// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.CommandLine;
using System.CommandLine.IO;

namespace Fluxzy.Cli.Commands
{
    public class OutputConsole : IConsole
    {
        public OutputConsole(IStandardStreamWriter @out, IStandardStreamWriter error)
        {
            Out = @out;
            Error = error;
        }

        public IStandardStreamWriter Out { get; }

        public bool IsOutputRedirected => false;

        public IStandardStreamWriter Error { get; }

        public bool IsErrorRedirected => false;

        public bool IsInputRedirected => false;
    }
}
