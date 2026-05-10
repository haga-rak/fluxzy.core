// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public sealed class CertListCommand : AsyncCommand
    {
        private readonly IFluxzyConsole _console;

        public CertListCommand(IFluxzyConsole console)
        {
            _console = console;
        }

        protected override Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
        {
            var certificateManager = new DefaultCertificateAuthorityManager();

            foreach (var certificate in certificateManager.EnumerateRootCertificates()) {
                _console.Out.Write($"{certificate.ThumbPrint}\t{certificate.Subject}" + Environment.NewLine);
            }

            return Task.FromResult(0);
        }
    }
}
