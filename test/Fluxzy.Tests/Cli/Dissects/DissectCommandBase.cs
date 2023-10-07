// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands;
using Fluxzy.Tests.Cli.Scaffolding;

namespace Fluxzy.Tests.Cli.Dissects
{
    public abstract class DissectCommandBase : IDisposable
    {
        private readonly OutputConsole _outputConsole;
        private readonly List<FileInfo> _tempFiles = new();

        protected DissectCommandBase()
        {
            var standardOutput = new OutputWriterNotifier();
            var standardError = new OutputWriterNotifier();
            _outputConsole = new OutputConsole(standardOutput, standardError, "");
        }

        protected async Task<RunResult> InternalRun(string fileName, params string[] options)
        {
            var args=  new[] { "dissect", fileName }.Concat(options).ToArray();
            var exitCode = await FluxzyStartup.Run(args, _outputConsole, CancellationToken.None);

            return new RunResult(exitCode, _outputConsole.BinaryStdout, _outputConsole.BinaryStderr);
        }

        protected FileInfo GetTempFile()
        {
            var tempFile = new FileInfo(Path.GetTempFileName());
            _tempFiles.Add(tempFile);

            return tempFile;
        }

        public void Dispose()
        {
            foreach (var tempFile in _tempFiles) {
                tempFile.Delete();
            }
        }
    }
}
