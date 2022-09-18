using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Interop.Pcap;
using Fluxzy.Saz;
using Microsoft.Extensions.CommandLineUtils;

namespace Fluxzy.Cli
{
    public class CliApp
    {
        private readonly Func<FluxzySetting, ICertificateProvider> _certificateProviderFactory;
        private readonly string _instanceIdentifier = Guid.NewGuid().ToString();
        private readonly string _tempDirectory;

        private readonly List<IDirectoryPackager> _packagers = new()
        {
            new FxzyDirectoryPackager(),
            new SazPackager(),
        };

        public CliApp(Func<FluxzySetting, ICertificateProvider> certificateProviderFactory)
        {
            _certificateProviderFactory = certificateProviderFactory;

            _tempDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"),
                "fxzy", _instanceIdentifier);
        }

        public int Start(string[] args)
        {
            Console.Title = string.Join(" ", args);

            CommandLineApplication commandLineApplication = new CommandLineApplication
            {
                FullName = "fxzy is a cli-app build around fluxzy engine.\r\n"
            };

            commandLineApplication.HelpOption("-h | --help");

            commandLineApplication.VersionOption("-v | --version", () =>
                $"\tFluxzy proxy engine version : {FileVersionInfo.GetVersionInfo(typeof(Proxy).Assembly.Location).ProductVersion} - created by Haga Rakotoharivelo");

            commandLineApplication
                .Command("start", OnProxyStartCommand);

            commandLineApplication
                .Command("certificate-dump", OnCertificateDump);

            commandLineApplication
                .Command("pack", OnPack);

            commandLineApplication.OnExecute(() =>
            {
                commandLineApplication.ShowHelp();
                return 0;
            });

            try
            {
                return commandLineApplication.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private void OnPack(CommandLineApplication target)
        {
            target.Description = "Pack an output directory to a specify format, supported fxyz, har, saz";

            target.HelpOption("-h | --help");

            var inputOption =
                target.Option("-i <inputDirectory>", "The directory to be packed", CommandOptionType.SingleValue);

            var outputOption =
                target.Option("-o <outputFile>", "Specify the file output", CommandOptionType.SingleValue);
            
            target.OnExecute(async () =>
            {
                if (!inputOption.HasValue() 
                    || string.IsNullOrWhiteSpace(inputOption.Value()))
                {
                    throw new ArgumentException("You must specify a valid input file");
                }

                if (!outputOption.HasValue() || string.IsNullOrWhiteSpace(outputOption.Value()))
                {
                    throw new ArgumentException("You must specify a valid output file");
                }

                var dInfo = new DirectoryInfo(inputOption.Value()); 

                if (!dInfo.Exists)
                {
                    throw new ArgumentException($"Directory\"{dInfo.FullName}\" does not exist");
                }
                
                await PackDirectoryToFile(dInfo, outputOption.Value());

                return 0;
            });
        }

        private void OnCertificateDump(CommandLineApplication target)
        {
            target.Description = "Export the default certificate used by fluxzy to a file";
            target.HelpOption("-h | --help");

            var argument = target.Argument("fileName", "Dump the default public certificate used by fluxzy to file");

            target.OnExecute(async () =>
            {
                if (string.IsNullOrWhiteSpace(argument.Value))
                {
                    throw new Exception($"You must specify an output fileName");
                }

                using (var stream = File.Create(argument.Value))
                {
                    await CertificateUtility.DumpDefaultCertificate(stream).ConfigureAwait(false);
                }

                return 0;
            });
        }

        private void OnProxyStartCommand(CommandLineApplication target)
        {
            target.FullName = "fxzy start";
            target.Description = "Start the proxy engine";

            target.HelpOption("-h | --help");

            var listenInterfaceOption = target.Option(
                "-l | --listen-iface <interface>",
                "Set up the binding address. Default value is 127.0.0.1:44344 which will listen to localhost on port 44344. 0.0.0.0:4434 to listen on all interface. You " +
                " can specify multiple values.",
                CommandOptionType.MultipleValue);

            var outputFileOption = target.Option(
                "-o | --output-file <fileName>",
                "Output the captured traffic to fileName",
                CommandOptionType.SingleValue);

            var outputDirectoryOption = target.Option(
                "-d | --output-dir <directoryName>",
                "Output the captured traffic to directory",
                CommandOptionType.SingleValue);

            var systemProxyOption = target.Option(
                "-sp",
                "Register fluxzy as system proxy when started",
                CommandOptionType.NoValue);

            var rawCaptureOption = target.Option(
                "-c",
                "Capture raw packets",
                CommandOptionType.NoValue);

            var skipSslDecryptionOption = target.Option(
                "--skip-ssl-decryption", "fluxzy will not try to decrypt ssl traffic.",
                CommandOptionType.NoValue);

            var skiptCertInstallOption = target.Option(
                "--skip-cert-install", "Do not register fluxzy certificate as root authority",
                CommandOptionType.NoValue);

            var noCertCacheOption = target.Option(
                "--no-cert-cache", "Do not build file system certificate cache.",
                CommandOptionType.NoValue);

            var secureProtocolsOption = target.Option(
                "--ssl-proto=<secureProtocols>", "Set ssl protocols. <secureProtocols> is among ssl3 tls tls11 and tls12",
                CommandOptionType.MultipleValue);

            var certificateFileOption = target.Option(
                "--cert-file=<certificatPfx>", "Set a compatible p12, pfx, or pkcs12 root CA certificate used by the SSL decryptor",
                CommandOptionType.SingleValue);

            var certificatePasswordOption = target.Option(
                "--cert-password=<certificatPfx>", "Set the password corresponding to --cert-file",
                CommandOptionType.SingleValue);

            var throttleOption = target.Option(
                "--throttleKb <throttleKb>", "Set bandwidth throttle in KiloByte per second",
                CommandOptionType.SingleValue);

            var throttleIntervalOption = target.Option(
                "--throttleInterval <intervalms>", "Set the throttling interval check. Default value is 50ms",
                CommandOptionType.SingleValue);

            target.OnExecute(async () =>
            {
                string outputFileName = string.Empty;

                var proxyStartUpSetting = FluxzySetting.CreateDefault();
                
                if (listenInterfaceOption.HasValue())
                {
                    foreach (var value in listenInterfaceOption.Values)
                    {
                        var splitedTab = value.Split(":"); 

                        if (splitedTab.Length < 2)
                            throw new Exception(
                                $"error in listen interface option \"{value}\", " +
                                $"string format must be address:port");

                        var address = string.Join(":", splitedTab.Take(splitedTab.Length - 1));

                        if (!int.TryParse(splitedTab.Last(), out var port) || (port < 1) ||
                            port > ushort.MaxValue)
                        {
                            throw new Exception("port must be a number between 1 and 65535");
                        }

                        proxyStartUpSetting.AddBoundAddress(address, port);
                    }
                }

                if (!proxyStartUpSetting.BoundPoints.Any())
                {
                    proxyStartUpSetting.AddBoundAddress("127.0.0.1", 44344);
                }

                if (outputDirectoryOption.HasValue())
                {
                    proxyStartUpSetting.SetArchivingPolicy(
                        ArchivingPolicy.CreateFromDirectory(
                            outputDirectoryOption.Value())); 
                }

                if (outputFileOption.HasValue())
                {
                    outputFileName = outputFileOption.Value();

                    if (proxyStartUpSetting.ArchivingPolicy == null ||
                        proxyStartUpSetting.ArchivingPolicy.Type != ArchivingPolicyType.Directory)
                    {
                        // Create a temporary directory 
                        Directory.CreateDirectory(_tempDirectory);
                        proxyStartUpSetting.SetArchivingPolicy(ArchivingPolicy.CreateFromDirectory(_tempDirectory));
                    }
                }
                

                if (throttleIntervalOption.HasValue())
                {
                    if (!int.TryParse(throttleIntervalOption.Value(), out var intervalMillis))
                    {
                        throw new Exception($"intervalMillis must be a number");
                    }

                    proxyStartUpSetting.SetThrottleIntervalCheck(TimeSpan.FromMilliseconds(intervalMillis));
                }

                if (secureProtocolsOption.HasValue() && secureProtocolsOption.Values.Any())
                {
                    SslProtocols sslProtocols = SslProtocols.None;

                    foreach (var secureProtocolRaw in secureProtocolsOption.Values)
                    {
                        if (!Enum.TryParse<SslProtocols>(secureProtocolRaw, out var parseResult))
                        {
                            throw new Exception($"{secureProtocolRaw} is not a valid value. secureProtocols must be among : ssl2, ssl3, tls, tls11, tls12.");
                        }

                        sslProtocols = sslProtocols | (SslProtocols)parseResult;
                    }

                    proxyStartUpSetting.SetServerProtocols(sslProtocols);
                }

                if (certificateFileOption.HasValue())
                {
                    if (!File.Exists(certificateFileOption.Value()))
                    {
                        throw new Exception($"Unable to read data from {certificateFileOption.Value()} file");
                    }

                    var password = certificatePasswordOption.HasValue() ? certificatePasswordOption.Value() : null;

                    proxyStartUpSetting.SetCaCertificate(
                        Certificate.LoadFromPkcs12(
                            File.ReadAllBytes(certificateFileOption.Value()), password ?? string.Empty));
                }

                proxyStartUpSetting.SetAutoInstallCertificate(!skiptCertInstallOption.HasValue());
                proxyStartUpSetting.SetSkipGlobalSslDecryption(skipSslDecryptionOption.HasValue());
                proxyStartUpSetting.SetDisableCertificateCache(noCertCacheOption.HasValue());

                try
                {
                    if (systemProxyOption.HasValue())
                    {
                        SystemProxyRegistration.Register(proxyStartUpSetting);
                    }

                    var sessionIdentifier = 
                        await StartBlockingProxy(proxyStartUpSetting, _certificateProviderFactory, rawCaptureOption.HasValue());

                    if (!string.IsNullOrWhiteSpace(outputFileName))
                    {
                        Console.WriteLine("Packing output ....");

                        await PackDirectoryToFile(new DirectoryInfo(Path.Combine(proxyStartUpSetting.ArchivingPolicy.Directory,
                                sessionIdentifier)),
                            outputFileName);

                        Console.WriteLine("Packing output done.");
                    }
                }
                finally
                {
                    if (Directory.Exists(_tempDirectory))
                        Directory.Delete(_tempDirectory, true);
                }


                return 0;
            });
        }

        private async Task<string> StartBlockingProxy(FluxzySetting startupSetting,
            Func<FluxzySetting, 
            ICertificateProvider> certificateProviderFactory, bool capturePcap)
        {
            var waitForExitStart = ConsoleHelper.WaitForExit();

            var statPrinter = new StatPrinter(Console.CursorTop, startupSetting.BoundPointsDescription);

            using var tcpConnectionProvider =
                capturePcap ? new CapturedTcpConnectionProvider() : ITcpConnectionProvider.Default;

            var proxy = new Proxy(startupSetting, certificateProviderFactory(startupSetting), tcpConnectionProvider);

            proxy.Run();

            await waitForExitStart.ConfigureAwait(false);

            statPrinter.Dispose();

            Console.WriteLine(@"Halting proxy ...");

            await proxy.DisposeAsync();

            Console.WriteLine(@"Proxy halted. Bye.");

            return proxy.SessionIdentifier; 
        }


        private async Task PackDirectoryToFile(DirectoryInfo dInfo, string outFileName)
        {
            var packager = _packagers.FirstOrDefault(p => p.ShouldApplyTo(outFileName));

            if (packager == null)
            {
                throw new ArgumentException(
                    $"Could not infer file format from output extension. Currently supported extension are : fxzy, har and saz");
            }

            await using var outStream = File.Create(outFileName);
            await packager.Pack(dInfo.FullName, outStream);
        }

    }
}