// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Extensions;
using Fluxzy.Har;
using Fluxzy.Interop.Pcap;
using Fluxzy.Interop.Pcap.Cli.Clients;
using Fluxzy.NativeOps.SystemProxySetup;
using Fluxzy.Rules;
using Fluxzy.Saz;

namespace Fluxzy.Cli.Commands
{
    public class StartCommandBuilder
    {
        private readonly string _instanceIdentifier;

        public readonly List<DirectoryPackager> _packagers = new() {
            new FxzyDirectoryPackager(),
            new SazPackager(),
            new HttpArchivePackager()
        };

        private DirectoryInfo _tempDumpDirectory;

        public StartCommandBuilder(string instanceIdentifier)
        {
            _instanceIdentifier = instanceIdentifier;
        }

        private DirectoryInfo TempDumpDirectory {
            get
            {
                if (_tempDumpDirectory != null)
                    return _tempDumpDirectory;

                var path = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"),
                    "fxzy", _instanceIdentifier);

                return _tempDumpDirectory = new DirectoryInfo(path);
            }
        }

        public Command Build(CancellationToken cancellationToken)
        {
            var command = new Command("start", "Start a capturing session");

            command.AddOption(CreateListenInterfaceOption());
            command.AddOption(CreateListenLocalhost());
            command.AddOption(CreateListToAllInterfaces());
            command.AddOption(CreateOutputFileOption());
            command.AddOption(CreateDumpToFolderOption());
            command.AddOption(CreateSystemProxyOption());
            command.AddOption(CreateTcpDumpOption());
            command.AddOption(CreateSkipSslOption());
            command.AddOption(CreateBouncyCastleOption());

            command.AddOption(CreateSkipCertInstallOption());
            command.AddOption(CreateNoCertCacheOption());
            command.AddOption(CreateCertificateFileOption());
            command.AddOption(CreateCertificatePasswordOption());
            command.AddOption(CreateRuleFileOption());
            command.AddOption(CreateRuleStdinOption());
            command.AddOption(CreateUaParsingOption());
            command.AddOption(CreateOutOfProcCaptureOption());
            command.AddOption(CreateProxyBuffer());
            command.AddOption(CreateCounterOption());

            command.SetHandler(context => Run(context, cancellationToken));

            return command;
        }

        public async Task Run(InvocationContext invocationContext, CancellationToken processToken)
        {
            var proxyStartUpSetting = FluxzySetting.CreateDefault();

            var listenInterfaces = invocationContext.Value<List<IPEndPoint>>("listen-interface");
            var listenLocalHost = invocationContext.Value<bool>("llo");
            var listenAnyInterfaces = invocationContext.Value<bool>("lany");
            var outFileInfo = invocationContext.Value<FileInfo>("output-file");
            var dumpDirectory = invocationContext.Value<DirectoryInfo>("dump-folder");
            var registerAsSystemProxy = invocationContext.Value<bool>("system-proxy");
            var includeTcpDump = invocationContext.Value<bool>("include-dump");
            var skipDecryption = invocationContext.Value<bool>("skip-ssl-decryption");
            var installCert = invocationContext.Value<bool>("install-cert");
            var noCertCache = invocationContext.Value<bool>("no-cert-cache");
            var certFile = invocationContext.Value<FileInfo>("cert-file");
            var certPassword = invocationContext.Value<string>("cert-password");
            var ruleFile = invocationContext.Value<FileInfo?>("rule-file");
            var ruleStdin = invocationContext.Value<bool>("rule-stdin");
            var parseUserAgent = invocationContext.Value<bool>("parse-ua");
            var outOfProcCapture = invocationContext.Value<bool>("external-capture");
            var bouncyCastle = invocationContext.Value<bool>("bouncy-castle");
            var requestBuffer = invocationContext.Value<int?>("request-buffer");
            var count = invocationContext.Value<int?>("max-capture-count");

            var invokeCancellationToken = invocationContext.GetCancellationToken();

            using var linkedTokenSource =
                processToken == default
                    ? CancellationTokenSource.CreateLinkedTokenSource(
                        invokeCancellationToken)
                    : CancellationTokenSource.CreateLinkedTokenSource(
                        processToken, invokeCancellationToken);

            if (requestBuffer.HasValue && requestBuffer >= 16)
                FluxzySharedSetting.RequestProcessingBuffer = requestBuffer.Value;

            var cancellationToken = linkedTokenSource.Token;

            proxyStartUpSetting.MaxExchangeCount = count; 
            proxyStartUpSetting.ClearBoundAddresses();

            var finalListenInterfaces = listenInterfaces.ToList();

            if (listenLocalHost) {
                finalListenInterfaces.Clear();
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.Loopback, 44344));
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.IPv6Loopback, 44344));
            }

            if (listenAnyInterfaces) {
                finalListenInterfaces.Clear();
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.Any, 44344));
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.IPv6Any, 44344));
            }

            foreach (var item in finalListenInterfaces) {
                proxyStartUpSetting.AddBoundAddress(item);
            }

            var archivingPolicy = dumpDirectory == null
                ? ArchivingPolicy.None
                : ArchivingPolicy.CreateFromDirectory(dumpDirectory);

            if (outFileInfo != null && archivingPolicy.Type == ArchivingPolicyType.None)
                archivingPolicy = ArchivingPolicy.CreateFromDirectory(TempDumpDirectory);

            if (certFile != null) {
                try {
                    var cert = Certificate.LoadFromPkcs12(
                        certFile.FullName,
                        certPassword ?? string.Empty);

                    proxyStartUpSetting.SetCaCertificate(cert);
                }
                catch (Exception ex) {
                    invocationContext.BindingContext.Console.WriteLine($"Error while reading cert-file : {ex.Message}");
                    invocationContext.ExitCode = 1;

                    return;
                }
            }

            var ruleContent = ruleStdin
                ? invocationContext.BindingContext.Console is OutputConsole oc
                    ? oc.StandardInputContent
                    : Console.In.ReadToEnd()
                : null;

            if (ruleContent == null && ruleFile != null) {
                if (!ruleFile.Exists)
                    throw new FileNotFoundException($"File not found : {ruleFile.FullName}");

                ruleContent = File.ReadAllText(ruleFile.FullName);
            }

            if (ruleContent != null) {
                try {
                    var ruleConfigParser = new RuleConfigParser();

                    var ruleSet = ruleConfigParser.TryGetRuleSetFromYaml(ruleContent,
                        out var errors);

                    if (ruleSet == null && errors!.Any())
                        throw new ArgumentException(string.Join("\r\n", errors.Select(s => s.Message)));

                    if (ruleSet != null)
                        proxyStartUpSetting.AlterationRules.AddRange(ruleSet.Rules.SelectMany(s => s.GetAllRules()));
                }
                catch (Exception ex) {
                    invocationContext.BindingContext.Console.WriteLine($"Error while reading rule file : {ex.Message}");
                    invocationContext.ExitCode = 1;

                    return;
                }
            }

            proxyStartUpSetting.SetArchivingPolicy(archivingPolicy);
            proxyStartUpSetting.SetAutoInstallCertificate(installCert);
            proxyStartUpSetting.SetSkipGlobalSslDecryption(skipDecryption);
            proxyStartUpSetting.SetDisableCertificateCache(noCertCache);
            proxyStartUpSetting.OutOfProcCapture = outOfProcCapture;
            proxyStartUpSetting.UseBouncyCastle = bouncyCastle;

            var certificateProvider = new CertificateProvider(proxyStartUpSetting,
                noCertCache ? new InMemoryCertificateCache() : new FileSystemCertificateCache(proxyStartUpSetting));

            proxyStartUpSetting.CaptureRawPacket = includeTcpDump;

            var uaParserProvider = parseUserAgent ? new UaParserUserAgentInfoProvider() : null;
            var systemProxyManager = new SystemProxyRegistrationManager(new NativeProxySetterManager().Get());

            await using var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            await using (var tcpConnectionProvider =
                         proxyStartUpSetting.CaptureRawPacket
                             ? await CapturedTcpConnectionProvider.Create(scope, proxyStartUpSetting.OutOfProcCapture)
                             : ITcpConnectionProvider.Default) {
                await using (var proxy = new Proxy(proxyStartUpSetting, certificateProvider,
                                 new DefaultCertificateAuthorityManager(), tcpConnectionProvider, uaParserProvider,
                                 externalCancellationSource: linkedTokenSource)) {
                    
                    var endPoints = proxy.Run();

                    invocationContext.BindingContext.Console
                                     .WriteLine($"Listen on {string.Join(", ", endPoints.Select(s => s))}");

                    if (registerAsSystemProxy) {
                        var setting = systemProxyManager.Register(endPoints, proxyStartUpSetting);

                        invocationContext.Console.Out.WriteLine(
                            $"Registered as system proxy on {setting.BoundHost}:{setting.ListenPort}");
                    }

                    invocationContext.Console.Out.WriteLine("Ready to process connections, Ctrl+C to exit.");

                    try {
                        await Task.Delay(-1, cancellationToken);
                    }
                    catch (OperationCanceledException) {
                    }
                    finally {
                        if (registerAsSystemProxy) {
                            systemProxyManager.UnRegister();
                            invocationContext.Console.Out.WriteLine("Unregistered as system proxy");
                        }
                    }
                }
            }

            invocationContext.Console.Out.WriteLine("Proxy ended, gracefully");

            if (outFileInfo != null) {
                invocationContext.Console.WriteLine($"Packing output to {outFileInfo.Name} ...");

                outFileInfo.Directory?.Create();

                await PackDirectoryToFile(
                    new DirectoryInfo(proxyStartUpSetting.ArchivingPolicy.Directory),
                    outFileInfo.FullName);

                invocationContext.Console.WriteLine("Packing output done.");
            }
        }

        private static Option CreateListenInterfaceOption()
        {
            var listenInterfaceOption = new Option<List<IPEndPoint>>(
                "--listen-interface",
                description:
                "Set up the binding addresses. " +
                "Default value is \"127.0.0.1/44344\" which will listen to localhost on port 44344. " +
                "0.0.0.0 to listen on all interface with default port." +
                " Accept multiple values.",
                isDefault: true,
                parseArgument: result => {
                    var listResult = new List<IPEndPoint>();

                    foreach (var token in result.Tokens) {
                        var tab = token.Value.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                        if (tab.Length == 1) {
                            if (!IPAddress.TryParse(tab.First(), out var ipAddress)) {
                                result.ErrorMessage = $"Invalid ip address {tab.First()}";

                                return null;
                            }

                            listResult.Add(new IPEndPoint(ipAddress, 44344));
                        }
                        else {
                            if (!IPAddress.TryParse(tab.First(), out var ipAddress)) {
                                result.ErrorMessage = $"Invalid ip address {tab.First()}";

                                return null;
                            }

                            var portString = string.Join("", tab.Skip(1));

                            if (!int.TryParse(portString, out var port)) {
                                result.ErrorMessage = $"Invalid port {portString}";

                                return null;
                            }

                            listResult.Add(new IPEndPoint(ipAddress, port));
                        }
                    }

                    return listResult;
                }
            );

            listenInterfaceOption.AddAlias("-l");
            listenInterfaceOption.SetDefaultValue(new List<IPEndPoint> { new(IPAddress.Loopback, 44344) });
            listenInterfaceOption.Arity = ArgumentArity.OneOrMore;

            return listenInterfaceOption;
        }

        private static Option CreateOutputFileOption()
        {
            var option = new Option<FileInfo?>(
                "--output-file",
                description: "Output the captured traffic to file",
                parseArgument: result => new FileInfo(result.Tokens.First().Value));

            option.AddAlias("-o");
            option.Arity = ArgumentArity.ExactlyOne;
            option.SetDefaultValue(null);

            return option;
        }

        private static Option CreateDumpToFolderOption()
        {
            var option = new Option<DirectoryInfo>(
                "--dump-folder",
                "Output the captured traffic to folder");

            option.AddAlias("-d");

            return option;
        }

        private static Option CreateSystemProxyOption()
        {
            var option = new Option<bool>(
                "--system-proxy",
                "Try to register fluxzy as system proxy when started");

            option.AddAlias("-sp");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateTcpDumpOption()
        {
            var option = new Option<bool>(
                "--include-dump",
                "Include tcp dumps on captured output");

            option.AddAlias("-c");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateSkipSslOption()
        {
            var option = new Option<bool>(
                "--skip-ssl-decryption",
                "Disable ssl traffic decryption");

            option.AddAlias("-ss");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateListenLocalhost()
        {
            var option = new Option<bool>(
                "--llo",
                "Listen on localhost address with default port. Same as -l 127.0.0.1/44344");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateListToAllInterfaces()
        {
            var option = new Option<bool>(
                "--lany",
                "Listen on all interfaces with default port (44344)");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateBouncyCastleOption()
        {
            var option = new Option<bool>(
                "--bouncy-castle",
                "Use Bouncy Castle as SSL/TLS provider");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateSkipCertInstallOption()
        {
            var option = new Option<bool>(
                "--install-cert",
                "Install root CA in current cert store (require higher privilege)");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateNoCertCacheOption()
        {
            var option = new Option<bool>(
                "--no-cert-cache",
                "Don't cache generated certificate on file system");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateUaParsingOption()
        {
            var option = new Option<bool>(
                "--parse-ua",
                "Parse user agent");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateOutOfProcCaptureOption()
        {
            var option = new Option<bool>(
                "--external-capture",
                "Indicates that the raw capture will be done by an external process");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        private static Option CreateCertificateFileOption()
        {
            var option = new Option<FileInfo>(
                "--cert-file",
                "Substitute the default CA certificate with a compatible PKCS#12 (p12, pfx) root CA certificate for SSL decryption");

            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }

        private static Option CreateCertificatePasswordOption()
        {
            var option = new Option<string>(
                "--cert-password",
                "Set the password corresponding to the certfile");

            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }

        private static Option CreateProxyBuffer()
        {
            var option = new Option<int?>(
                "--request-buffer",
                "Set the default request buffer"
            );

            option.Arity = ArgumentArity.ExactlyOne;
            option.SetDefaultValue(null);

            return option;
        }

        private static Option CreateRuleFileOption()
        {
            var option = new Option<FileInfo>(
                "--rule-file",
                "Use a fluxzy rule file. See more at : https://www.fluxzy.io/docs/concept/rule-configuration-file");

            option.AddAlias("-r");
            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }
        private static Option CreateCounterOption()
        {
            var option = new Option<int?>(
                "--max-capture-count",
                "Exit after count exchange");

            option.AddAlias("-n");
            option.SetDefaultValue(null);
            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }

        private static Option CreateRuleStdinOption()
        {
            var option = new Option<bool>(
                "--rule-stdin",
                "Read rule from stdin");

            option.AddAlias("-R");
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public async Task PackDirectoryToFile(DirectoryInfo dInfo, string outFileName)
        {
            var packager = _packagers.FirstOrDefault(p => p.ShouldApplyTo(outFileName));

            if (packager == null) {
                throw new ArgumentException(
                    "Could not infer file format from output extension. Currently supported extension are : fxzy, har and saz");
            }

            await using var outStream = File.Create(outFileName);
            await packager.Pack(dInfo.FullName, outStream, null);
        }
    }
}
