using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Echoes.Core;
using Microsoft.Extensions.CommandLineUtils;

namespace Echoes.Cli
{
    public class CliApp
    {
        private readonly Func<ProxyStartupSetting, ICertificateProvider> _certificateProviderFactory;

        public CliApp(Func<ProxyStartupSetting, ICertificateProvider> certificateProviderFactory)
        {
            _certificateProviderFactory = certificateProviderFactory;
        }

        public int Start(string[] args)
        {
            Console.Title = string.Join(" ", args);

            CommandLineApplication commandLineApplication = new CommandLineApplication
            {
                FullName = "echoescli is a command line tool which let you use features of the echoes proxy engine."
            };

            commandLineApplication.HelpOption("-h | --help");

            commandLineApplication.VersionOption("-v | --version", () =>
            {
                return
                    "\tEchoes proxy engine version : " +
                    FileVersionInfo.GetVersionInfo(typeof(Proxy).Assembly.Location).ProductVersion;
            });

            commandLineApplication
                .Command("start", OnProxyStartCommand);

            commandLineApplication
                .Command("certificate-dump", OnCertificateDump);

            commandLineApplication
                .Command("parse", OnParse);

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

        private void OnParse(CommandLineApplication target)
        {
            target.Description = "Parse the default *.ecz format to other format";
            target.HelpOption("-h | --help");

            var inputOption =
                target.Option("-i <eczFile>", "Specify the .ecz input file to parse", CommandOptionType.SingleValue);

            var outputOption =
                target.Option("-o <outputFile>", "Specify the file output", CommandOptionType.SingleValue);

            // TODO uncommunet when HAR is set up 
            //var formatOption = 
            //    target.Option("-f <format>", "Specify the file format among har and saz", CommandOptionType.SingleValue);

            target.OnExecute(async () =>
            {
                if (!inputOption.HasValue() || string.IsNullOrWhiteSpace(inputOption.Value()))
                {
                    throw new ArgumentException("You must specify a valid input file");
                }

                if (!outputOption.HasValue() || string.IsNullOrWhiteSpace(outputOption.Value()))
                {
                    throw new ArgumentException("You must specify a valid output file");
                }

               // await ExportUtility.ConvertToSazFile(inputOption.Value(), outputOption.Value()).ConfigureAwait(false);

                return 0;
            });

        }

        private void OnCertificateDump(CommandLineApplication target)
        {
            target.Description = "Export the default certificate used by echoes to file";
            target.HelpOption("-h | --help");

            var argument = target.Argument("fileName", "Dump the default public certificate used by echoes to a file");

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

        private void OnProxyStartCommand(CommandLineApplication target
          )
        {
            target.FullName = "echoescli start";
            target.Description = "Start the proxy engine";

            target.HelpOption("-h | --help");

            var portOption = target.Option(
                "-p | --port <portnumber>",
                "Set up the proxy listen port",
                CommandOptionType.SingleValue);

            var listenInterfaceOption = target.Option(
                "-l | --listen-iface <interface>",
                "Set up the binding address. Default value is loopback. 0.0.0.0 to listen on all interface",
                CommandOptionType.SingleValue);

            var outputFileOption = target.Option(
                "-o | --output-file <fileName>",
                "Output the captured traffic to fileName",
                CommandOptionType.SingleValue);

            var systemProxyOption = target.Option(
                "-sp",
                "Register echoes as system proxy when started",
                CommandOptionType.NoValue);

            var skipSslDecryptionOption = target.Option(
                "--skip-ssl-decryption", "echoes will not try to decrypt ssl traffic.",
                CommandOptionType.NoValue);

            var skiptCertInstallOption = target.Option(
                "--skip-cert-install", "Do not register the certificate as authority",
                CommandOptionType.NoValue);

            var noCertCacheOption = target.Option(
                "--no-cert-cache", "Do not build file system certificate cache.",
                CommandOptionType.NoValue);

            var secureProtocolsOption = target.Option(
                "--ssl-proto=<secureProtocols>", "Set ssl protocols. <secureProtocols> is among ssl3 tls tls11 and tls12",
                CommandOptionType.MultipleValue);

            var certificateFileOption = target.Option(
                "--cert-file=<certificatPfx>", "Set a compatible p12, pfx, or pkcs12 root certificate used by the SSL decryptor",
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

                var proxyStartUpSetting = ProxyStartupSetting.CreateDefault();

                if (portOption.HasValue())
                {
                    if (!int.TryParse(portOption.Value(), out var port) || (port < 1) ||
                        port > ushort.MaxValue)
                    {
                        throw new Exception("port must be a number between 1 and 65535");
                    }

                    proxyStartUpSetting.SetListenPort(port);
                }

                if (listenInterfaceOption.HasValue())
                {
                    proxyStartUpSetting.SetBoundAddress(listenInterfaceOption.Value());
                }

                if (outputFileOption.HasValue())
                {
                    outputFileName = outputFileOption.Value();
                }

                if (throttleOption.HasValue())
                {
                    if (!int.TryParse(throttleOption.Value(), out var throttleKb))
                    {
                        throw new Exception($"throttleKb must be a number");
                    }

                    proxyStartUpSetting.SetThrottleKoPerSecond(throttleKb);

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

                    proxyStartUpSetting.SetSecureCertificate(File.ReadAllBytes(certificateFileOption.Value()), password ?? string.Empty);

                }

                proxyStartUpSetting.SetAutoInstallCertificate(!skiptCertInstallOption.HasValue());
                proxyStartUpSetting.SetSkipSslDecryption(skipSslDecryptionOption.HasValue());
                proxyStartUpSetting.SetAsSystemProxy(systemProxyOption.HasValue());
                proxyStartUpSetting.SetDisableCertificateCache(noCertCacheOption.HasValue());

                //proxyStartUpSetting.SetClientCertificateConfiguration(
                //    new ClientCertificateConfiguration(){
                //    ClientSettings = { new ClientConfigItem()
                //    {
                //        HostNames = new List<string>() { "*.staging.bnpparibas"},
                //        CertificateSerialNumber = "56f4fe08b6e489150ecbf18414f326c8",
                //    },
                //        new ClientConfigItem()
                //        {
                //            HostNames = { "*.lcl.fr"},
                //            CertificateSerialNumber = "00b90d117253cb6c6d39e14b26"
                //        }


                //    }
                //});
                    //proxyStartUpSetting.SetConnectionPerHost(16);

                await StartBlockingProxy(proxyStartUpSetting, outputFileName, _certificateProviderFactory);


                return 0;
            });
        }

        private async Task StartBlockingProxy(ProxyStartupSetting startupSetting, string outputFileName,
            Func<ProxyStartupSetting, ICertificateProvider> certificateProviderFactory)
        {
            var echoArchiveFile = (string) null;

            var statPrinter = new StatPrinter(Console.CursorTop, startupSetting.BoundAddress, startupSetting.ListenPort);

            async Task OnNewExchange(Exchange exchange)
            {
                await statPrinter.OnNewExchange(exchange).ConfigureAwait(false);

                //if (echoArchiveFile != null)
                //{
                //    await echoArchiveFile.Append(exchange).ConfigureAwait(false);
                //}
            }

            var proxy = new Proxy(startupSetting, certificateProviderFactory(startupSetting), OnNewExchange);

            proxy.Run();

            await ConsoleHelper.WaitForExit().ConfigureAwait(false);

            statPrinter.Dispose();

            Console.WriteLine(@"Halting proxy ...");

            await proxy.Release().ConfigureAwait(false);
            proxy.Dispose();

            Console.WriteLine(@"Proxy halted. Bye.");
        }
    }
}