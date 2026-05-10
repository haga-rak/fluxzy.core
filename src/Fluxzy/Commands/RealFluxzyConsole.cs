// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

namespace Fluxzy.Cli.Commands
{
    internal sealed class RealFluxzyConsole : IFluxzyConsole
    {
        public IConsoleWriter Out { get; } = new TextWriterConsoleWriter(Console.Out);

        public IConsoleWriter Error { get; } = new TextWriterConsoleWriter(Console.Error);

        public Stream BinaryStdout { get; } = Console.OpenStandardOutput();

        public Stream BinaryStderr { get; } = Console.OpenStandardError();

        public string? StandardInputContent => null;

        private sealed class TextWriterConsoleWriter : IConsoleWriter
        {
            private readonly TextWriter _writer;

            public TextWriterConsoleWriter(TextWriter writer)
            {
                _writer = writer;
            }

            public void Write(string? value)
            {
                _writer.Write(value);
            }
        }
    }
}
