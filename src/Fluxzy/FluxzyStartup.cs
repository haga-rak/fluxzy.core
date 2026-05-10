// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Cli.Commands;
using Fluxzy.Cli.Commands.SpectreInfra;
using Fluxzy.Core;
using Spectre.Console.Cli;

namespace Fluxzy.Cli
{
    public static class FluxzyStartup
    {
        public static async Task<int> Run(string[] args, OutputConsole? outputConsole, CancellationToken token,
            EnvironmentProvider? environmentProvider = null)
        {
            var currentEnvironmentProvider = environmentProvider ?? new SystemEnvironmentProvider();

            if (currentEnvironmentProvider.GetEnvironmentVariable("FLUXZY_STDOUT_ARGS") == "1") {
                if (outputConsole == null) {
                    Console.WriteLine(string.Join(" ", args));
                }
                else {
                    outputConsole.Out.Write(string.Join(" ", args) + Environment.NewLine);
                }
            }

            // Spectre.Console.Cli has no built-in --version. Intercept it before the parser
            // sees `-v` (which is `cert create --validity` and would conflict at root).
            if (args.Length == 1 && args[0] == "--version") {
                var version = typeof(FluxzyStartup).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? typeof(FluxzyStartup).Assembly.GetName().Version?.ToString()
                    ?? "unknown";

                if (outputConsole == null) {
                    Console.WriteLine(version);
                }
                else {
                    outputConsole.Out.Write(version + Environment.NewLine);
                }

                return 0;
            }

            return await RunViaSpectre(args, outputConsole, currentEnvironmentProvider, token);
        }

        private static async Task<int> RunViaSpectre(string[] args, OutputConsole? outputConsole,
            EnvironmentProvider environmentProvider, CancellationToken token)
        {
            IFluxzyConsole fluxzyConsole = outputConsole is { } oc ? oc : new RealFluxzyConsole();

            args = RewriteSpectreArgs(args);

            var registrar = new TypeRegistrar();
            registrar.RegisterInstance(typeof(IFluxzyConsole), fluxzyConsole);
            registrar.RegisterInstance(typeof(EnvironmentProvider), environmentProvider);
            registrar.Register(typeof(StartCommand), typeof(StartCommand));
            registrar.Register(typeof(DissectCommand), typeof(DissectCommand));
            registrar.Register(typeof(CertCheckCommand), typeof(CertCheckCommand));
            registrar.Register(typeof(CertListCommand), typeof(CertListCommand));
            registrar.Register(typeof(CertDefaultCommand), typeof(CertDefaultCommand));

            var app = new CommandApp(registrar);

            app.Configure(cfg => {
                cfg.SetApplicationName("fluxzy");

                cfg.AddCommand<StartCommand>("start")
                   .WithDescription("Start a capturing session");

                cfg.AddCommand<PackCommand>("pack")
                   .WithDescription("Export a fluxzy result directory to a specific archive format");

                cfg.AddCommand<DissectCommand>("dissect")
                   .WithAlias("dis")
                   .WithDescription("Read content of a previously captured file or directory.");

                cfg.AddCommand<DissectPcapCommand>(DissectPcapInternalName)
                   .IsHidden();

                cfg.AddBranch<CommandSettings>("cert", branch => {
                    branch.SetDescription("Manage root certificates used by the fluxzy");
                    branch.AddCommand<CertExportCommand>("export")
                          .WithDescription("Export the default embedded certificate used by fluxzy");
                    branch.AddCommand<CertInstallCommand>("install")
                          .WithDescription("Trust a certificate as ROOT (need elevation)");
                    branch.AddCommand<CertCheckCommand>("check")
                          .WithDescription("Check if the provided certificate (or embedded if omit) is trusted");
                    branch.AddCommand<CertUninstallCommand>("uninstall")
                          .WithDescription("Remove a certificate from Root CA authority store");
                    branch.AddCommand<CertListCommand>("list")
                          .WithDescription("List all root certificates");
                    branch.AddCommand<CertCreateCommand>("create")
                          .WithDescription("Create a self-signed root CA certificate in PKCS#12 format");
                    branch.AddCommand<CertDefaultCommand>("default")
                          .WithDescription(
                              "Get or set the default root CA for the current user. Environment variable FLUXZY_ROOT_CERTIFICATE overrides this setting.");
                }).WithAlias("certificate");

                cfg.SetExceptionHandler((ex, _) => {
                    fluxzyConsole.Error.Write(ex.Message + Environment.NewLine);

                    return 1;
                });
            });

            return await app.RunAsync(args, token);
        }

        // Spectre.Console.Cli doesn't natively support "command that takes positional args AND
        // has subcommands". `dissect` needs both: `dissect <input>` runs the dissection, while
        // `dissect pcap <input> -o <out>` runs pcap export. We rewrite the second form to a
        // hidden top-level command name before handing off to Spectre. The user-facing CLI is
        // unchanged.
        private const string DissectPcapInternalName = "__dissect-pcap";

        private static string[] RewriteSpectreArgs(string[] args)
        {
            if (args.Length >= 2
                && (args[0] == "dissect" || args[0] == "dis")
                && args[1] == "pcap") {
                var rewritten = new string[args.Length - 1];
                rewritten[0] = DissectPcapInternalName;
                Array.Copy(args, 2, rewritten, 1, args.Length - 2);

                return rewritten;
            }

            // Spectre 0.55.0 rejects long-form options of length 1 (e.g. "--O", "--L", "--C").
            // The legacy `cert create` CLI accepts these uppercase/lowercase single-letter
            // long-forms; rewrite them to the short single-dash form so Spectre can bind.
            if (args.Length >= 2
                && (args[0] == "cert" || args[0] == "certificate")
                && args[1] == "create") {
                var rewritten = (string[]) args.Clone();

                for (var i = 2; i < rewritten.Length; i++) {
                    var token = rewritten[i];

                    if (token.Length == 3 && token[0] == '-' && token[1] == '-' && IsCertCreateAliasChar(token[2])) {
                        rewritten[i] = "-" + token[2];
                    }
                }

                return rewritten;
            }

            return args;
        }

        private static bool IsCertCreateAliasChar(char c)
        {
            return c == 'O' || c == 'o' || c == 'L' || c == 'l' || c == 'C' || c == 'c';
        }
    }
}
