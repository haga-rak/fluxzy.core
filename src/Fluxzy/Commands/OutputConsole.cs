// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;

namespace Fluxzy.Cli.Commands
{
    public class OutputConsole
    {
        public OutputConsole(TextWriter @out, TextWriter error, string? standardInputContent)
        {
            Out = @out;
            Error = error;
            StandardInputContent = standardInputContent;
        }

        public string? StandardInputContent { get; }

        public TextWriter Out { get; }

        public TextWriter Error { get; }

        public MemoryStream BinaryStdout { get; } = new();

        public MemoryStream BinaryStderr { get; } = new();

        public static OutputConsole CreateEmpty()
        {
            return new OutputConsole(TextWriter.Null, TextWriter.Null, null);
        }
    }
}
