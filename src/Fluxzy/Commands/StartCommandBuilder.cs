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
using Fluxzy.Archiving.Har;
using Fluxzy.Archiving.Saz;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Cli.Clients;
using Fluxzy.Extensions;
using Fluxzy.Misc.Traces;
using Fluxzy.Rules;
using Fluxzy.Utils.NativeOps.SystemProxySetup;

namespace Fluxzy.Cli.Commands
{
    public class StartCommandBuilder
    {
        private readonly string _instanceIdentifier;

        public readonly List<DirectoryPackager> Packagers = new() {
            new FxzyDirectoryPackager(),
            new SazPackager(),
            new HttpArchivePackager()
        };

        private DirectoryInfo _tempDumpDirectory = null!;

        /// <summary>
        /// </summary>
        /// <param name="instanceIdentifier"></param>
        public StartCommandBuilder(string instanceIdentifier)
        {
            _instanceIdentifier = instanceIdentifier;
        }

        private DirectoryInfo TempDumpDirectory {
            get
            {
                if (_tempDumpDirectory != null) {
                    return _tempDumpDirectory;
                }

                var path = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"),
                    "fluxzy.cli", _instanceIdentifier);

                return _tempDumpDirectory = new DirectoryInfo(path);
            }
        }

        public Command Build(CancellationToken cancellationToken)
        {
            var command = new Command("start", "Start a capturing session");
            
            command.AddOption(StartCommandOptions.CreateListenInterfaceOption());
            command.AddOption(StartCommandOptions.CreateListenLocalhost());
            command.AddOption(StartCommandOptions.CreateListToAllInterfaces());
            command.AddOption(StartCommandOptions.CreateOutputFileOption());
            command.AddOption(StartCommandOptions.CreateDumpToFolderOption());
            command.AddOption(StartCommandOptions.CreateRuleFileOption());
            command.AddOption(StartCommandOptions.CreateSystemProxyOption());
            command.AddOption(StartCommandOptions.CreateBouncyCastleOption());
            command.AddOption(StartCommandOptions.CreateTcpDumpOption());
            command.AddOption(StartCommandOptions.CreateSkipSslOption());
            command.AddOption(StartCommandOptions.CreateEnableTracingOption());

            command.AddOption(StartCommandOptions.CreateSkipCertInstallOption());
            command.AddOption(StartCommandOptions.CreateNoCertCacheOption());
            command.AddOption(StartCommandOptions.CreateCertificateFileOption());
            command.AddOption(StartCommandOptions.CreateCertificatePasswordOption());
            command.AddOption(StartCommandOptions.CreateRuleStdinOption());
            command.AddOption(StartCommandOptions.CreateUaParsingOption());
            command.AddOption(StartCommandOptions.CreateUser502Option());
            command.AddOption(StartCommandOptions.CreateOutOfProcCaptureOption());
            command.AddOption(StartCommandOptions.CreateProxyBuffer());
            command.AddOption(StartCommandOptions.CreateCounterOption());

            command.SetHandler(context => Run(context, cancellationToken));

            return command;
        }

        public async Task Run(InvocationContext invocationContext, CancellationToken processToken)
        {
            var proxyStartUpSetting = FluxzySetting.CreateDefault();

            var listenInterfaces = invocationContext.Value<List<IPEndPoint>>("listen-interface");
            var listenLocalHost = invocationContext.Value<bool>("llo");
            var listenAnyInterfaces = invocationContext.Value<bool>("lany");
            var outFileInfo = invocationContext.Value<FileInfo?>("output-file");
            var dumpDirectory = invocationContext.Value<DirectoryInfo?>("dump-folder");
            var registerAsSystemProxy = invocationContext.Value<bool>("system-proxy");
            var includeTcpDump = invocationContext.Value<bool>("include-dump");
            var skipDecryption = invocationContext.Value<bool>("skip-ssl-decryption");
            var installCert = invocationContext.Value<bool>("install-cert");
            var noCertCache = invocationContext.Value<bool>("no-cert-cache");
            var certFile = invocationContext.Value<FileInfo?>("cert-file");
            var certPassword = invocationContext.Value<string>("cert-password");
            var ruleFile = invocationContext.Value<FileInfo?>("rule-file");
            var ruleStdin = invocationContext.Value<bool>("rule-stdin");
            var parseUserAgent = invocationContext.Value<bool>("parse-ua");
            var outOfProcCapture = invocationContext.Value<bool>("external-capture");
            var bouncyCastle = invocationContext.Value<bool>("bouncy-castle");
            var requestBuffer = invocationContext.Value<int?>("request-buffer");
            var count = invocationContext.Value<int?>("max-capture-count");
            var trace = invocationContext.Value<bool>("trace");
            var use502 = invocationContext.Value<bool>("use-502");

            if (trace) {
                D.EnableTracing = true;
            }

            FluxzySharedSetting.Use528 = !use502;

            var invokeCancellationToken = invocationContext.GetCancellationToken();

            using var linkedTokenSource =
                processToken == default
                    ? CancellationTokenSource.CreateLinkedTokenSource(
                        invokeCancellationToken)
                    : CancellationTokenSource.CreateLinkedTokenSource(
                        processToken, invokeCancellationToken);

            if (requestBuffer.HasValue && requestBuffer >= 16) {
                FluxzySharedSetting.RequestProcessingBuffer = requestBuffer.Value;
            }

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

            if (outFileInfo != null && archivingPolicy.Type == ArchivingPolicyType.None) {
                archivingPolicy = ArchivingPolicy.CreateFromDirectory(TempDumpDirectory);
            }

            if (certFile != null) {
                try {
                    var cert = Certificate.LoadFromPkcs12(
                        certFile.FullName,
                        certPassword ?? string.Empty!);

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
                    : await Console.In.ReadToEndAsync(cancellationToken)
                : null;

            if (ruleContent == null && ruleFile != null) {
                if (!ruleFile.Exists) {
                    throw new FileNotFoundException($"File not found : {ruleFile.FullName}");
                }

                ruleContent = File.ReadAllText(ruleFile.FullName);
            }

            if (ruleContent != null) {
                try {
                    var ruleConfigParser = new RuleConfigParser();

                    var ruleSet = ruleConfigParser.TryGetRuleSetFromYaml(ruleContent,
                        out var errors);

                    if (ruleSet == null && errors!.Any()) {
                        throw new ArgumentException(string.Join("\r\n", errors!.Select(s => s.Message)));
                    }

                    if (ruleSet != null) {
                        proxyStartUpSetting.AddAlterationRules(ruleSet.Rules.SelectMany(s => s.GetAllRules()));
                    }
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

            var certificateProvider = new CertificateProvider(proxyStartUpSetting.CaCertificate,
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
                        var setting = await systemProxyManager.Register(endPoints, proxyStartUpSetting);

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
                            try {
                                await systemProxyManager.UnRegister();
                            }
                            catch (Exception ex) {
                                invocationContext.Console.Error.WriteLine(
                                    $"Failed to unregister as system proxy : {ex.Message}");
                            }

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
                    new DirectoryInfo(proxyStartUpSetting.ArchivingPolicy.Directory!),
                    outFileInfo.FullName);

                invocationContext.Console.WriteLine("Packing output done.");
            }
        }

        private async Task PackDirectoryToFile(DirectoryInfo dInfo, string outFileName)
        {
            var packager = Packagers.FirstOrDefault(p => p.ShouldApplyTo(outFileName));

            if (packager == null) {
                throw new ArgumentException(
                    "Could not infer file format from output extension. Currently supported extension are : fxzy, har and saz");
            }

            await using var outStream = File.Create(outFileName);
            await packager.Pack(dInfo.FullName, outStream, null);
        }
    }
}
