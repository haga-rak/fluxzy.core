// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.ComponentModel;
using Fluxzy.Cli.Commands;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class StartSettings : CommandSettings
    {
        [CommandOption("-l|--listen-interface")]
        [Description(
            "Set up the binding addresses. " +
            "Default value is \"127.0.0.1:44344\" which will listen to localhost on port 44344. " +
            "0.0.0.0 to listen on all interface with the default port. Use port 0 to let OS assign a random available port. " +
            "Accepts multiple values.")]
        public string[]? ListenInterface { get; set; }

        [CommandOption("--llo")]
        [Description("Listen on localhost address with default port. Same as -l 127.0.0.1/44344")]
        public bool ListenLocalhost { get; set; }

        [CommandOption("--lany")]
        [Description("Listen on all interfaces with default port (44344)")]
        public bool ListenAny { get; set; }

        [CommandOption("-o|--output-file")]
        [Description("Output the captured traffic to an archive file")]
        public string? OutputFile { get; set; }

        [CommandOption("-d|--dump-folder")]
        [Description("Output the captured traffic to folder")]
        public string? DumpFolder { get; set; }

        [CommandOption("-r|--rule-file")]
        [Description("Use a fluxzy rule file. See more at : https://www.fluxzy.io/resources/documentation/the-rule-file")]
        public string? RuleFile { get; set; }

        [CommandOption("-R|--rule-stdin")]
        [Description("Read rule from stdin")]
        public bool RuleStdin { get; set; }

        [CommandOption("--system-proxy")]
        [Description("Try to register fluxzy as system proxy when started")]
        public bool SystemProxy { get; set; }

        [CommandOption("-k|--insecure")]
        [Description(
            "Skip remote certificate validation globally. Use `SkipRemoteCertificateValidationAction` for specific host only")]
        public bool Insecure { get; set; }

        [CommandOption("--skip-ssl-decryption")]
        [Description("Disable ssl traffic decryption")]
        public bool SkipSslDecryption { get; set; }

        [CommandOption("-b|--bouncy-castle")]
        [Description("Use Bouncy Castle as SSL/TLS provider")]
        public bool BouncyCastle { get; set; }

        [CommandOption("-c|--include-dump")]
        [Description("Include tcp dumps on captured output")]
        public bool IncludeDump { get; set; }

        [CommandOption("--external-capture")]
        [Description("Indicates that the raw capture will be done by an external process")]
        public bool ExternalCapture { get; set; }

        [CommandOption("-i|--install-cert")]
        [Description("Install root CA in current cert store if absent (require higher privilege)")]
        public bool InstallCert { get; set; }

        [CommandOption("--no-cert-cache")]
        [Description("Don't cache generated certificate on file system")]
        public bool NoCertCache { get; set; }

        [CommandOption("--cert-file")]
        [Description(
            "Substitute the default CA certificate with a compatible PKCS#12 (p12, pfx) root CA certificate for SSL decryption")]
        public string? CertFile { get; set; }

        [CommandOption("--cert-password")]
        [Description("Set the password of certfile if any")]
        public string? CertPassword { get; set; }

        [CommandOption("--parse-ua")]
        [Description("Parse user agent")]
        public bool ParseUa { get; set; }

        [CommandOption("--use-502")]
        [Description("Use 502 status code for upstream error instead of 528.")]
        public bool Use502 { get; set; }

        [CommandOption("--mode")]
        [Description("Set proxy mode")]
        [DefaultValue(ProxyMode.Regular)]
        public ProxyMode Mode { get; set; } = ProxyMode.Regular;

        [CommandOption("--mode-reverse-port")]
        [Description("Set the remote authority port when --mode ReverseSecure or --mode ReversePlain is set")]
        public int? ModeReversePort { get; set; }

        [CommandOption("--proxy-auth-basic")]
        [Description(
            "Require a basic authentication. Username and password shall be provided in this format: username:password. " +
            "Values can be provided in a percent encoded format.")]
        public string? ProxyAuthBasic { get; set; }

        [CommandOption("--request-buffer")]
        [Description("Set the default request buffer")]
        public int? RequestBuffer { get; set; }

        [CommandOption("--max-upstream-connection")]
        [Description("Maximum connection per upstream host")]
        public int MaxUpstreamConnection { get; set; } = FluxzySharedSetting.MaxConnectionPerHost;

        [CommandOption("-n|--max-capture-count")]
        [Description("Exit after a specified count of exchanges")]
        public int? MaxCaptureCount { get; set; }

        [CommandOption("--enable-process-tracking")]
        [Description(
            "Enable tracking of the local process that initiated each request. Only works for connections originating from localhost.")]
        public bool EnableProcessTracking { get; set; }

        [CommandOption("--no-android-emulator")]
        [Description(
            "Disable inclusion of Android emulator host (10.0.2.2) in self detection. " +
            "By default, Fluxzy considers 10.0.2.2 as a local address for Android emulator compatibility.")]
        public bool NoAndroidEmulator { get; set; }

        [CommandOption("-p|--pretty")]
        [Description("Enable interactive pretty output with live exchange table and statistics panel")]
        public bool Pretty { get; set; }

        [CommandOption("--pretty-max-rows")]
        [Description("Maximum number of exchanges to keep in the pretty output buffer")]
        [DefaultValue(2000)]
        public int PrettyMaxRows { get; set; } = 2000;

        [CommandOption("--serve-h2")]
        [Description(
            "Enable HTTP/2 on the proxy's client-facing (downstream) side. " +
            "When enabled, clients that support HTTP/2 over TLS will use it to communicate with the proxy.")]
        public bool ServeH2 { get; set; }

        [CommandOption("--enable-discovery")]
        [Description(
            "Enable mDNS discovery service to announce the proxy on the local network. " +
            "Allows clients to discover the proxy automatically.")]
        public bool EnableDiscovery { get; set; }

        [CommandOption("--proto-dir")]
        [Description("Directories containing .proto files for gRPC/protobuf decoding. Accepts multiple values.")]
        public string[]? ProtoDir { get; set; }

        [CommandOption("-t|--trace [VALUE]")]
        [Description(
            "Emit Fluxzy diagnostic logs to the console. " +
            "Without a value, logs at Debug level. Use '-t deep' for verbose (Trace) level.")]
        public FlagValue<string?> Trace { get; set; } = new();

        [CommandOption("--skip-internal-rules")]
        [Description("Do not add Fluxzy's built-in rules (welcome page, /ca certificate endpoint)")]
        public bool SkipInternalRules { get; set; }
    }
}
