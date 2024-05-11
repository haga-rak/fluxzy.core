// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;

namespace Fluxzy.Cli.Commands
{
    public class OutputConsole : IConsole
    {
        public OutputConsole(IStandardStreamWriter @out, IStandardStreamWriter error, string? standardInputContent)
        {
            Out = @out;
            Error = error;
            StandardInputContent = standardInputContent;
        }

        public string? StandardInputContent { get; }

        public IStandardStreamWriter Out { get; }

        public bool IsOutputRedirected => false;

        public IStandardStreamWriter Error { get; }

        public bool IsErrorRedirected => false;

        public bool IsInputRedirected => false;

        public MemoryStream BinaryStdout { get; } = new();

        public MemoryStream BinaryStderr { get; } = new();

        public static OutputConsole CreateEmpty()
        {
            return new OutputConsole(EmptyWriter, EmptyWriter, null);
        }

        private static IStandardStreamWriter EmptyWriter { get; } = new NullStreamWriter();
        
        class NullStreamWriter : IStandardStreamWriter
        {
            public void Write(string? value)
            {
            }
        }
    }
}
