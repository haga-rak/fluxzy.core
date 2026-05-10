// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Archiving.Har;
using Fluxzy.Archiving.Saz;
using Fluxzy.Certificates;
using Fluxzy.Cli.Commands.PrettyOutput;
using Fluxzy.Core;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Cli.Clients;
using Fluxzy.Extensions;
using Fluxzy.Rules;
using Fluxzy.Utils;
using Fluxzy.Utils.NativeOps.SystemProxySetup;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public sealed class StartCommand : AsyncCommand<StartSettings>
    {
        private readonly IFluxzyConsole _console;

        private readonly List<DirectoryPackager> _packagers = new() {
            new FxzyDirectoryPackager(),
            new SazPackager(),
            new HttpArchivePackager()
        };

        private DirectoryInfo? _tempDumpDirectory;

        public StartCommand(IFluxzyConsole console)
        {
            _console = console;
        }

        protected override async Task<int> ExecuteAsync(CommandContext context, StartSettings settings,
            CancellationToken cancellationToken)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            ConsoleCancelEventHandler? cancelHandler = null;

            // Console.CancelKeyPress: production SIGINT handling. CancellationToken.None is the
            // production default (Program.Main passes it), so we must wire Ctrl+C ourselves.
            // Tests provide their own token and cancel programmatically; the handler is harmless.
            try {
                cancelHandler = (_, e) => {
                    e.Cancel = true;

                    try {
                        linkedTokenSource.Cancel();
                    }
                    catch (ObjectDisposedException) {
                    }
                };
                Console.CancelKeyPress += cancelHandler;
            }
            catch {
                cancelHandler = null;
            }

            try {
                return await RunCore(settings, linkedTokenSource);
            }
            finally {
                if (cancelHandler != null) {
                    try {
                        Console.CancelKeyPress -= cancelHandler;
                    }
                    catch {
                    }
                }
            }
        }

        private async Task<int> RunCore(StartSettings settings, CancellationTokenSource linkedTokenSource)
        {
            var proxyStartUpSetting = FluxzySetting.CreateDefault();

            FluxzySharedSetting.Use528 = !settings.Use502;

            if (settings.RequestBuffer is { } reqBuf && reqBuf >= 16) {
                FluxzySharedSetting.RequestProcessingBuffer = reqBuf;
            }

            FluxzySharedSetting.MaxConnectionPerHost = settings.MaxUpstreamConnection;

            var cancellationToken = linkedTokenSource.Token;

            proxyStartUpSetting.MaxExchangeCount = settings.MaxCaptureCount;
            proxyStartUpSetting.ClearBoundAddresses();

            if (settings.Mode == ProxyMode.ReverseSecure) {
                if (settings.SystemProxy) {
                    throw new ArgumentException("Cannot register as system proxy when using reverse mode");
                }

                proxyStartUpSetting.SetReverseMode(true);

                if (settings.ModeReversePort != null) {
                    proxyStartUpSetting.SetReverseModeForcedPort(settings.ModeReversePort.Value);
                }
            }

            if (settings.Mode == ProxyMode.ReversePlain) {
                if (settings.SystemProxy) {
                    throw new ArgumentException("Cannot register as system proxy when using reverse mode");
                }

                proxyStartUpSetting.SetReverseMode(true);
                proxyStartUpSetting.SetReverseModePlainHttp(true);

                if (settings.ModeReversePort != null) {
                    proxyStartUpSetting.SetReverseModeForcedPort(settings.ModeReversePort.Value);
                }
            }

            if (!string.IsNullOrEmpty(settings.ProxyAuthBasic)) {
                var credential = ParseProxyAuth(settings.ProxyAuthBasic);

                if (credential == null) {
                    return 1;
                }

                proxyStartUpSetting.SetProxyAuthentication(
                    ProxyAuthentication.Basic(credential.UserName, credential.Password));
            }

            if (settings.Insecure) {
                proxyStartUpSetting.SetSkipRemoteCertificateValidation(true);
            }

            var finalListenInterfaces = new List<IPEndPoint>();

            if (settings.ListenInterface is { Length: > 0 }) {
                foreach (var raw in settings.ListenInterface) {
                    if (!AuthorityUtility.TryParseIp(raw, out var ip, out var port)) {
                        _console.Error.Write($"Invalid listen value address {raw}" + Environment.NewLine);

                        return 1;
                    }

                    finalListenInterfaces.Add(new IPEndPoint(ip!, port));
                }
            }
            else {
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.Loopback, 44344));
            }

            if (settings.ListenLocalhost) {
                finalListenInterfaces.Clear();
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.Loopback, 44344));
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.IPv6Loopback, 44344));
            }

            if (settings.ListenAny) {
                finalListenInterfaces.Clear();
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.Any, 44344));
                finalListenInterfaces.Add(new IPEndPoint(IPAddress.IPv6Any, 44344));
            }

            foreach (var item in finalListenInterfaces) {
                proxyStartUpSetting.AddBoundAddress(item);
            }

            DirectoryInfo? dumpDirectory = string.IsNullOrEmpty(settings.DumpFolder)
                ? null
                : new DirectoryInfo(settings.DumpFolder);

            FileInfo? outFileInfo = string.IsNullOrEmpty(settings.OutputFile)
                ? null
                : new FileInfo(settings.OutputFile);

            var archivingPolicy = dumpDirectory == null
                ? ArchivingPolicy.None
                : ArchivingPolicy.CreateFromDirectory(dumpDirectory);

            if (outFileInfo != null && archivingPolicy.Type == ArchivingPolicyType.None) {
                archivingPolicy = ArchivingPolicy.CreateFromDirectory(GetTempDumpDirectory());
            }

            if (!string.IsNullOrEmpty(settings.CertFile)) {
                try {
                    var cert = Certificate.LoadFromPkcs12(
                        settings.CertFile,
                        settings.CertPassword ?? string.Empty);

                    proxyStartUpSetting.SetCaCertificate(cert);
                }
                catch (Exception ex) {
                    _console.Out.Write($"Error while reading cert-file : {ex.Message}" + Environment.NewLine);

                    return 1;
                }
            }

            FileInfo? ruleFile = string.IsNullOrEmpty(settings.RuleFile)
                ? null
                : new FileInfo(settings.RuleFile);

            var ruleContent = settings.RuleStdin
                ? await ReadStdinForRules(cancellationToken)
                : null;

            if (ruleContent == null && ruleFile == null) {
                ruleFile = DefaultRuleFileHelper.FindDefaultRuleFile();
            }

            if (ruleContent == null && ruleFile != null) {
                if (!ruleFile.Exists) {
                    throw new FileNotFoundException($"File not found : {ruleFile.FullName}");
                }

                ruleContent = await File.ReadAllTextAsync(ruleFile.FullName, cancellationToken);
            }

            if (ruleContent != null) {
                try {
                    var ruleConfigParser = new RuleConfigParser();

                    var ruleSet = ruleConfigParser.TryGetRuleSetFromYaml(ruleContent, out var errors);

                    if (ruleSet == null && errors!.Any()) {
                        throw new ArgumentException(string.Join("\r\n", errors!.Select(s => s.Message)));
                    }

                    if (ruleSet != null) {
                        proxyStartUpSetting.AddAlterationRules(ruleSet.Rules.SelectMany(s => s.GetAllRules()));
                    }
                }
                catch (Exception ex) {
                    _console.Out.Write($"Error while reading rule file : {ex.Message}" + Environment.NewLine);

                    return 1;
                }
            }

            proxyStartUpSetting.SetArchivingPolicy(archivingPolicy);
            proxyStartUpSetting.SetAutoInstallCertificate(settings.InstallCert);
            proxyStartUpSetting.SetSkipGlobalSslDecryption(settings.SkipSslDecryption);
            proxyStartUpSetting.SetDisableCertificateCache(settings.NoCertCache);
            // When FLUXZY_SUDO_PASSWORD_FILE is set we can't capture in-process (the current
            // process has no caps — the whole point of the file is to sudo the child). Force
            // out-of-proc so the fluxzynetcap helper gets spawned under sudo.
            proxyStartUpSetting.OutOfProcCapture = settings.ExternalCapture
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FLUXZY_SUDO_PASSWORD_FILE"));
            proxyStartUpSetting.UseBouncyCastle = settings.BouncyCastle;
            proxyStartUpSetting.SetEnableProcessTracking(settings.EnableProcessTracking);
            proxyStartUpSetting.SetIncludeAndroidEmulatorHost(!settings.NoAndroidEmulator);
            proxyStartUpSetting.SetServeH2(settings.ServeH2);
            proxyStartUpSetting.SetEnableDiscoveryService(settings.EnableDiscovery);
            proxyStartUpSetting.SetSkipInternalRules(settings.SkipInternalRules);

            if (settings.ProtoDir is { Length: > 0 }) {
                foreach (var dir in settings.ProtoDir) {
                    proxyStartUpSetting.AddProtoDirectory(dir);
                }
            }

            var certificateProvider = new CertificateProvider(proxyStartUpSetting.CaCertificate,
                settings.NoCertCache
                    ? new InMemoryCertificateCache()
                    : new FileSystemCertificateCache(proxyStartUpSetting));

            proxyStartUpSetting.CaptureRawPacket = settings.IncludeDump;

            var uaParserProvider = settings.ParseUa ? new UaParserUserAgentInfoProvider() : null;
            var systemProxyManager = new SystemProxyRegistrationManager(new NativeProxySetterManager().Get());

            using var loggerFactory = CreateTraceLoggerFactory(ResolveTraceMode(settings));

            // Scope owns the out-of-proc capture subprocess lifetime. It must be disposed
            // BEFORE PackDirectoryToFile runs so the subprocess closes its pcapng FileStreams
            // and flushes all buffered packet data to disk; otherwise small captures can sit
            // in the 4 KB FileStream buffer and the packager's Length==0 skip drops them.
            await using (var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                             a => new OutOfProcessCaptureContext(a))) {

                if (!ValidateSetting(proxyStartUpSetting)) {
                    return 1;
                }

                await using (var tcpConnectionProvider = proxyStartUpSetting.CaptureRawPacket
                                 ? await CapturedTcpConnectionProvider.Create(scope, proxyStartUpSetting.OutOfProcCapture)
                                 : ITcpConnectionProvider.Default) {
                    await using (var proxy = new Proxy(proxyStartUpSetting, certificateProvider,
                                     new DefaultCertificateAuthorityManager(), tcpConnectionProvider, uaParserProvider,
                                     externalCancellationSource: linkedTokenSource,
                                     loggerFactory: loggerFactory)) {

                        var endPoints = proxy.Run();

                        _console.Out.Write(
                            $"Listen on {string.Join(", ", endPoints.Select(s => s))}" + Environment.NewLine);

                        if (settings.SystemProxy) {
                            var setting = await systemProxyManager.Register(endPoints, proxyStartUpSetting);

                            _console.Out.Write(
                                $"Registered as system proxy on {setting.BoundHost}:{setting.ListenPort}"
                                + Environment.NewLine);
                        }

                        if (settings.Pretty) {
                            await using var renderer = new PrettyOutputRenderer(
                                proxyStartUpSetting, settings.PrettyMaxRows, cancellationToken);
                            renderer.SubscribeToProxy(proxy);

                            try {
                                await renderer.RunAsync();
                            }
                            catch (OperationCanceledException) {
                            }
                            finally {
                                renderer.UnsubscribeFromProxy(proxy);

                                if (settings.SystemProxy) {
                                    try {
                                        await systemProxyManager.UnRegister();
                                    }
                                    catch (Exception ex) {
                                        _console.Error.Write(
                                            $"Failed to unregister as system proxy : {ex.Message}"
                                            + Environment.NewLine);
                                    }

                                    _console.Out.Write("Unregistered as system proxy" + Environment.NewLine);
                                }
                            }
                        }
                        else {
                            _console.Out.Write("Ready to process connections, Ctrl+C to exit." + Environment.NewLine);

                            try {
                                await Task.Delay(-1, cancellationToken);
                            }
                            catch (OperationCanceledException) {
                            }
                            finally {
                                if (settings.SystemProxy) {
                                    try {
                                        await systemProxyManager.UnRegister();
                                    }
                                    catch (Exception ex) {
                                        _console.Error.Write(
                                            $"Failed to unregister as system proxy : {ex.Message}"
                                            + Environment.NewLine);
                                    }

                                    _console.Out.Write("Unregistered as system proxy" + Environment.NewLine);
                                }
                            }
                        }
                    }
                }
            } // scope dispose: subprocess exits, pcapng FileStreams closed + flushed

            _console.Out.Write("Proxy ended, gracefully" + Environment.NewLine);

            if (outFileInfo != null) {
                _console.Out.Write($"Packing output to {outFileInfo.Name} ..." + Environment.NewLine);

                outFileInfo.Directory?.Create();

                await PackDirectoryToFile(
                    new DirectoryInfo(proxyStartUpSetting.ArchivingPolicy.Directory!),
                    outFileInfo.FullName);

                _console.Out.Write("Packing output done." + Environment.NewLine);
            }

            return 0;
        }

        private DirectoryInfo GetTempDumpDirectory()
        {
            if (_tempDumpDirectory != null) {
                return _tempDumpDirectory;
            }

            var path = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"),
                "fluxzy.cli", Guid.NewGuid().ToString());

            return _tempDumpDirectory = new DirectoryInfo(path);
        }

        private async Task<string?> ReadStdinForRules(CancellationToken cancellationToken)
        {
            if (_console.StandardInputContent != null) {
                return _console.StandardInputContent;
            }

            return await Console.In.ReadToEndAsync(cancellationToken);
        }

        private NetworkCredential? ParseProxyAuth(string raw)
        {
            var parts = raw.Split(':');

            if (parts.Length == 1) {
                _console.Error.Write("Username and password must be separated by with column." + Environment.NewLine);

                return null;
            }

            if (parts.Length > 2) {
                _console.Error.Write(
                    "Provided credentials contains multiple columns. Use %3A for column in username or password."
                    + Environment.NewLine);

                return null;
            }

            return new NetworkCredential(WebUtility.UrlDecode(parts[0]), WebUtility.UrlDecode(parts[1]));
        }

        private static TraceMode ResolveTraceMode(StartSettings settings)
        {
            if (!settings.Trace.IsSet) {
                return TraceMode.None;
            }

            var value = settings.Trace.Value;

            if (string.IsNullOrEmpty(value)) {
                return TraceMode.Debug;
            }

            if (string.Equals(value, "deep", StringComparison.OrdinalIgnoreCase)) {
                return TraceMode.Deep;
            }

            if (string.Equals(value, "debug", StringComparison.OrdinalIgnoreCase)) {
                return TraceMode.Debug;
            }

            return TraceMode.Debug;
        }

        private static ILoggerFactory? CreateTraceLoggerFactory(TraceMode traceMode)
        {
            if (traceMode == TraceMode.None) {
                return null;
            }

            var minimumLevel = traceMode == TraceMode.Deep ? LogLevel.Trace : LogLevel.Debug;

            return LoggerFactory.Create(builder => {
                builder.SetMinimumLevel(minimumLevel);
                builder.AddFilter("Fluxzy", minimumLevel);
                builder.AddSimpleConsole(options => {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss.fff ";
                });
            });
        }

        private bool ValidateSetting(FluxzySetting proxyStartUpSetting)
        {
            var validationResults = AggregateFluxzySettingAnalyzer.Instance.Validate(proxyStartUpSetting).ToList();

            if (validationResults.Any()) {
                foreach (var validationResult in validationResults) {
                    _console.WriteValidationResult(validationResult);
                }

                if (validationResults.Any(v => v.Level == ValidationRuleLevel.Fatal)) {
                    return false;
                }
            }

            return true;
        }

        private async Task PackDirectoryToFile(DirectoryInfo dInfo, string outFileName)
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
