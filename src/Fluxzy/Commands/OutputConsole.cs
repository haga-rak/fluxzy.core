// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;

namespace Fluxzy.Cli.Commands
{
    public class OutputConsole : IFluxzyConsole
    {
        public OutputConsole(IConsoleWriter @out, IConsoleWriter error, string? standardInputContent)
        {
            Out = @out;
            Error = error;
            StandardInputContent = standardInputContent;
        }

        public string? StandardInputContent { get; }

        public IConsoleWriter Out { get; }

        public IConsoleWriter Error { get; }

        public MemoryStream BinaryStdout { get; } = new();

        public MemoryStream BinaryStderr { get; } = new();

        Stream IFluxzyConsole.BinaryStdout => BinaryStdout;

        Stream IFluxzyConsole.BinaryStderr => BinaryStderr;

        public static OutputConsole CreateEmpty()
        {
            return new OutputConsole(EmptyWriter, EmptyWriter, null);
        }

        private static IConsoleWriter EmptyWriter { get; } = new NullConsoleWriter();

        private sealed class NullConsoleWriter : IConsoleWriter
        {
            public void Write(string? value)
            {
            }
        }
    }
}
