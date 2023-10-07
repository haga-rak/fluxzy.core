// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net;
using Fluxzy.Utils;

namespace Fluxzy.Cli.Commands
{
    public static class StartCommandOptions
    {

        public static Option CreateListenInterfaceOption()
        {
            var listenInterfaceOption = new Option<List<IPEndPoint>>(
                "--listen-interface",
                description:
                "Set up the binding addresses. " +
                "Default value is \"127.0.0.1:44344\" which will listen to localhost on port 44344. " +
                "0.0.0.0 to listen on all interface with default port. Use port 0 to let OS affect a random available port." +
                " Accept multiple values.",
                isDefault: true,
                parseArgument: result => {
                    var listResult = new List<IPEndPoint>();

                    foreach (var token in result.Tokens)
                    {
                        if (!AuthorityUtility.TryParseIp(token.Value, out var ipAddress, out var port))
                        {
                            result.ErrorMessage = $"Invalid listen value address {token.Value}";
                            return null!;
                        }

                        listResult.Add(new IPEndPoint(ipAddress!, port));
                    }

                    return listResult;
                }
            );

            listenInterfaceOption.AddAlias("-l");
            listenInterfaceOption.SetDefaultValue(new List<IPEndPoint> { new(IPAddress.Loopback, 44344) });
            listenInterfaceOption.Arity = ArgumentArity.OneOrMore;

            return listenInterfaceOption;
        }

        public static Option CreateOutputFileOption()
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

        public static Option CreateDumpToFolderOption()
        {
            var option = new Option<DirectoryInfo>(
                "--dump-folder",
                "Output the captured traffic to folder");

            option.AddAlias("-d");

            return option;
        }

        public static Option CreateSystemProxyOption()
        {
            var option = new Option<bool>(
                "--system-proxy",
                "Try to register fluxzy as system proxy when started");

            option.AddAlias("-sp");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateTcpDumpOption()
        {
            var option = new Option<bool>(
                "--include-dump",
                "Include tcp dumps on captured output");

            option.AddAlias("-c");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateSkipSslOption()
        {
            var option = new Option<bool>(
                "--skip-ssl-decryption",
                "Disable ssl traffic decryption");

            option.AddAlias("-ss");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateListenLocalhost()
        {
            var option = new Option<bool>(
                "--llo",
                "Listen on localhost address with default port. Same as -l 127.0.0.1/44344");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateListToAllInterfaces()
        {
            var option = new Option<bool>(
                "--lany",
                "Listen on all interfaces with default port (44344)");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateBouncyCastleOption()
        {
            var option = new Option<bool>(
                "--bouncy-castle",
                "Use Bouncy Castle as SSL/TLS provider");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateSkipCertInstallOption()
        {
            var option = new Option<bool>(
                "--install-cert",
                "Install root CA in current cert store if absent (require higher privilege)");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateNoCertCacheOption()
        {
            var option = new Option<bool>(
                "--no-cert-cache",
                "Don't cache generated certificate on file system");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }


        public static Option CreateUaParsingOption()
        {
            var option = new Option<bool>(
                "--parse-ua",
                "Parse user agent");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateUser502Option()
        {
            var option = new Option<bool>(
                "--use-502",
                "Use 502 status code for upstream error instead of 528.");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateOutOfProcCaptureOption()
        {
            var option = new Option<bool>(
                "--external-capture",
                "Indicates that the raw capture will be done by an external process");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateCertificateFileOption()
        {
            var option = new Option<FileInfo>(
                "--cert-file",
                "Substitute the default CA certificate with a compatible PKCS#12 (p12, pfx) root CA certificate for SSL decryption");

            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }

        public static Option CreateCertificatePasswordOption()
        {
            var option = new Option<string>(
                "--cert-password",
                "Set the password of certfile if any");

            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }

        public static Option CreateProxyBuffer()
        {
            var option = new Option<int?>(
                "--request-buffer",
                "Set the default request buffer"
            );

            option.Arity = ArgumentArity.ExactlyOne;
            option.SetDefaultValue(null);

            return option;
        }

        public static Option CreateRuleFileOption()
        {
            var option = new Option<FileInfo>(
                "--rule-file",
                "Use a fluxzy rule file. See more at : https://www.fluxzy.io/docs/concept/rule-configuration-file");

            option.AddAlias("-r");
            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }

        public static Option CreateCounterOption()
        {
            var option = new Option<int?>(
                "--max-capture-count",
                "Exit after count exchange");

            option.AddAlias("-n");
            option.SetDefaultValue(null);
            option.Arity = ArgumentArity.ExactlyOne;

            return option;
        }

        public static Option CreateRuleStdinOption()
        {
            var option = new Option<bool>(
                "--rule-stdin",
                "Read rule from stdin");

            option.AddAlias("-R");
            option.Arity = ArgumentArity.Zero;

            return option;
        }

        public static Option CreateEnableTracingOption()
        {
            var option = new Option<bool>(
                "--trace",
                "Output trace on stdout");

            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option;
        }

    }
}
