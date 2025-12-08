// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.CommandLine;
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
                if (_tempDumpDirectory != null!) {
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

            command.Options.Add(StartCommandOptions.CreateListenLocalhost());
            command.Options.Add(StartCommandOptions.CreateListToAllInterfaces());
            command.Options.Add(StartCommandOptions.CreateListenInterfaceOption());
            command.Options.Add(StartCommandOptions.CreateOutputFileOption());
            command.Options.Add(StartCommandOptions.CreateDumpToFolderOption());
            command.Options.Add(StartCommandOptions.CreateRuleFileOption());
            command.Options.Add(StartCommandOptions.CreateRuleStdinOption());
            command.Options.Add(StartCommandOptions.CreateSystemProxyOption());
            command.Options.Add(StartCommandOptions.CreateSkipRemoteCertificateValidation());
            command.Options.Add(StartCommandOptions.CreateSkipSslOption());
            command.Options.Add(StartCommandOptions.CreateBouncyCastleOption());
            command.Options.Add(StartCommandOptions.CreateTcpDumpOption());
            command.Options.Add(StartCommandOptions.CreateOutOfProcCaptureOption());
            command.Options.Add(StartCommandOptions.CreateEnableTracingOption());
            command.Options.Add(StartCommandOptions.CreateSkipCertInstallOption());
            command.Options.Add(StartCommandOptions.CreateNoCertCacheOption());
            command.Options.Add(StartCommandOptions.CreateCertificateFileOption());
            command.Options.Add(StartCommandOptions.CreateCertificatePasswordOption());
            command.Options.Add(StartCommandOptions.CreateUaParsingOption());
            command.Options.Add(StartCommandOptions.CreateUser502Option());
            command.Options.Add(StartCommandOptions.CreateReverseProxyMode());
            command.Options.Add(StartCommandOptions.CreateReverseProxyModePortOption());
            command.Options.Add(StartCommandOptions.CreateProxyAuthenticationOption());
            command.Options.Add(StartCommandOptions.CreateProxyBuffer());
            command.Options.Add(StartCommandOptions.CreateMaxConnectionPerHost());
            command.Options.Add(StartCommandOptions.CreateCounterOption());

            command.SetAction(async (parseResult, invokeCancellationToken) => {
                await Run(parseResult, cancellationToken, invokeCancellationToken);
            });

            return command;
        }

        public async Task Run(ParseResult parseResult, CancellationToken processToken, CancellationToken invokeCancellationToken)
        {
            var stdout = parseResult.InvocationConfiguration?.Output ?? Console.Out;
            var stderr = parseResult.InvocationConfiguration?.Error ?? Console.Error;

            var proxyStartUpSetting = FluxzySetting.CreateDefault();

            var listenInterfaces = parseResult.Value<List<IPEndPoint>>("listen-interface");
            var listenLocalHost = parseResult.Value<bool>("llo");
            var listenAnyInterfaces = parseResult.Value<bool>("lany");
            var outFileInfo = parseResult.Value<FileInfo?>("output-file");
            var dumpDirectory = parseResult.Value<DirectoryInfo?>("dump-folder");
            var registerAsSystemProxy = parseResult.Value<bool>("system-proxy");
            var skipRemoteCertificateValidation = parseResult.Value<bool>("insecure");
            var skipDecryption = parseResult.Value<bool>("skip-ssl-decryption");
            var includeTcpDump = parseResult.Value<bool>("include-dump");
            var installCert = parseResult.Value<bool>("install-cert");
            var noCertCache = parseResult.Value<bool>("no-cert-cache");
            var certFile = parseResult.Value<FileInfo?>("cert-file");
            var certPassword = parseResult.Value<string?>("cert-password");
            var ruleFile = parseResult.Value<FileInfo?>("rule-file");
            var ruleStdin = parseResult.Value<bool>("rule-stdin");
            var parseUserAgent = parseResult.Value<bool>("parse-ua");
            var outOfProcCapture = parseResult.Value<bool>("external-capture");
            var bouncyCastle = parseResult.Value<bool>("bouncy-castle");
            var requestBuffer = parseResult.Value<int?>("request-buffer");
            var maxConnectionPerHost = parseResult.Value<int>("max-upstream-connection");
            var count = parseResult.Value<int?>("max-capture-count");
            var trace = parseResult.Value<bool>("trace");
            var use502 = parseResult.Value<bool>("use-502");
            var proxyMode = parseResult.Value<ProxyMode>("mode");
            var modeReversePort = parseResult.Value<int?>("mode-reverse-port");
            var proxyBasicAuthCredential = parseResult.Value<NetworkCredential?>("proxy-auth-basic");

            if (trace) {
                D.EnableTracing = true;
            }

            FluxzySharedSetting.Use528 = !use502;

            using var linkedTokenSource =
                processToken == default
                    ? CancellationTokenSource.CreateLinkedTokenSource(
                        invokeCancellationToken)
                    : CancellationTokenSource.CreateLinkedTokenSource(
                        processToken, invokeCancellationToken);

            if (requestBuffer.HasValue && requestBuffer >= 16) {
                FluxzySharedSetting.RequestProcessingBuffer = requestBuffer.Value;
            }

            FluxzySharedSetting.MaxConnectionPerHost = maxConnectionPerHost;

            var cancellationToken = linkedTokenSource.Token;

            proxyStartUpSetting.MaxExchangeCount = count;
            proxyStartUpSetting.ClearBoundAddresses();

            if (proxyMode == ProxyMode.ReverseSecure) {
                if (registerAsSystemProxy) {
                    throw new ArgumentException("Cannot register as system proxy when using reverse mode");
                }

                proxyStartUpSetting.SetReverseMode(true);

                if (modeReversePort != null) {
                    proxyStartUpSetting.SetReverseModeForcedPort(modeReversePort.Value);
                }
            }

            if (proxyMode == ProxyMode.ReversePlain) {
                if (registerAsSystemProxy) {
                    throw new ArgumentException("Cannot register as system proxy when using reverse mode");
                }

                proxyStartUpSetting.SetReverseMode(true);
                proxyStartUpSetting.SetReverseModePlainHttp(true);

                if (modeReversePort != null) {
                    proxyStartUpSetting.SetReverseModeForcedPort(modeReversePort.Value);
                }
            }

            if (proxyBasicAuthCredential != null) {
                proxyStartUpSetting.SetProxyAuthentication(
                    ProxyAuthentication.Basic(proxyBasicAuthCredential.UserName, proxyBasicAuthCredential.Password));
            }

            if (skipRemoteCertificateValidation) {
                proxyStartUpSetting.SetSkipRemoteCertificateValidation(true);
            }

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
                    await stderr.WriteLineAsync($"Error while reading cert-file : {ex.Message}");
                    Environment.ExitCode = 1;

                    return;
                }
            }

            var ruleContent = ruleStdin
                ? await Console.In.ReadToEndAsync(cancellationToken)
                : null;

            if (ruleContent == null && ruleFile == null) {
                // Try to find default rule file on current directory
                ruleFile = DefaultRuleFileHelper.FindDefaultRuleFile();
            }

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
                    await stderr.WriteLineAsync($"Error while reading rule file : {ex.Message}");
                    Environment.ExitCode = 1;

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

            await using var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(), a => new OutOfProcessCaptureContext(a));

            if (!ValidateSetting(stderr, proxyStartUpSetting)) {
                Environment.ExitCode = 1;

                return;
            }

            await using (var tcpConnectionProvider = proxyStartUpSetting.CaptureRawPacket
                             ? await CapturedTcpConnectionProvider.Create(scope, proxyStartUpSetting.OutOfProcCapture)
                             : ITcpConnectionProvider.Default) {
                await using (var proxy = new Proxy(proxyStartUpSetting, certificateProvider,
                                 new DefaultCertificateAuthorityManager(), tcpConnectionProvider, uaParserProvider,
                                 externalCancellationSource: linkedTokenSource)) {
                    var endPoints = proxy.Run();

                    await stdout.WriteLineAsync($"Listen on {string.Join(", ", endPoints.Select(s => s))}");

                    if (registerAsSystemProxy) {
                        var setting = await systemProxyManager.Register(endPoints, proxyStartUpSetting);

                        await stdout.WriteLineAsync(
                            $"Registered as system proxy on {setting.BoundHost}:{setting.ListenPort}");
                    }

                    await stdout.WriteLineAsync("Ready to process connections, Ctrl+C to exit.");

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
                                await stderr.WriteLineAsync(
                                    $"Failed to unregister as system proxy : {ex.Message}");
                            }

                            await stdout.WriteLineAsync("Unregistered as system proxy");
                        }
                    }
                }
            }

            await stdout.WriteLineAsync("Proxy ended, gracefully");

            if (outFileInfo != null) {
                await stdout.WriteLineAsync($"Packing output to {outFileInfo.Name} ...");

                outFileInfo.Directory?.Create();

                await PackDirectoryToFile(
                    new DirectoryInfo(proxyStartUpSetting.ArchivingPolicy.Directory!),
                    outFileInfo.FullName);

                await stdout.WriteLineAsync("Packing output done.");
            }
        }

        private static bool ValidateSetting(TextWriter stderr, FluxzySetting proxyStartUpSetting)
        {
            var validationResults = AggregateFluxzySettingAnalyzer.Instance.Validate(proxyStartUpSetting).ToList();

            if (validationResults.Any()) {
                foreach (var validationResult in validationResults) {
                    stderr.WriteValidationResult(validationResult);
                }

                if (validationResults.Any(v => v.Level == ValidationRuleLevel.Fatal)) {
                    return false;
                }
            }

            return true;
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

    internal static class DefaultRuleFileHelper
    {
        public static readonly string[] DefaultRuleFileNames = { "fluxzy-rule.yaml", "fluxzy-rule.yml" };

        public static FileInfo? FindDefaultRuleFile()
        {
            foreach (var item in DefaultRuleFileNames) {
                var file = new FileInfo(item);

                if (file.Exists) {
                    return file;
                }
            }

            return null;
        }
    }
}
