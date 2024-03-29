// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli;
using Fluxzy.Cli.Commands;
using Fluxzy.Core;
using Fluxzy.Tests.Cli.Dissects;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public abstract class CommandBase : IDisposable
    {
        private readonly string _commandName;
        private readonly bool _hook;
        private readonly OutputConsole _outputConsole;
        private readonly List<FileInfo> _tempFiles = new();

        protected CommandBase(string commandName, bool hook = false)
        {
            _commandName = commandName;
            _hook = hook;
            var standardOutput = new OutputWriterNotifier(hook);
            var standardError = new OutputWriterNotifier(hook);
            _outputConsole = new OutputConsole(standardOutput, standardError, "");
        }

        protected async Task<RunResult> InternalRun(params string[] options)
        {
            return await InternalRun(new SystemEnvironmentProvider(), options);
        }

        protected async Task<RunResult> InternalRun(EnvironmentProvider environmentProvider, 
            params string[] options)
        {
            var args = new[] { _commandName }.Concat(options).ToArray();

            var exitCode = await FluxzyStartup.Run(args, _outputConsole, CancellationToken.None,
                environmentProvider);

            return new RunResult(exitCode,
                _outputConsole.BinaryStdout,
                _outputConsole.BinaryStderr,
                _hook ? ((OutputWriterNotifier)_outputConsole.Out).GetOutput() : null,
                _hook ? ((OutputWriterNotifier)_outputConsole.Error).GetOutput() : null);
        }

        protected FileInfo GetTempFile()
        {
            var tempFile = new FileInfo(Path.GetTempFileName());
            _tempFiles.Add(tempFile);

            return tempFile;
        }

        public void Dispose()
        {
            foreach (var tempFile in _tempFiles)
            {
                tempFile.Delete();
            }
        }
    }
}
