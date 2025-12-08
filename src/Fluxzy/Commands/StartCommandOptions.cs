// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
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
                "--listen-interface") {
                Description = "Set up the binding addresses. " +
                    "Default value is \"127.0.0.1:44344\" which will listen to localhost on port 44344. " +
                    "0.0.0.0 to listen on all interface with the default port. Use port 0 to let OS assign a random available port." +
                    " Accepts multiple values.",
                Arity = ArgumentArity.OneOrMore,
                DefaultValueFactory = _ => new List<IPEndPoint> { new(IPAddress.Loopback, 44344) },
                CustomParser = result => {
                    var listResult = new List<IPEndPoint>();

                    foreach (var token in result.Tokens) {
                        if (!AuthorityUtility.TryParseIp(token.Value, out var ipAddress, out var port)) {
                            result.AddError($"Invalid listen value address {token.Value}");

                            return null!;
                        }

                        listResult.Add(new IPEndPoint(ipAddress!, port));
                    }

                    return listResult;
                }
            };

            listenInterfaceOption.Aliases.Add("-l");

            return listenInterfaceOption;
        }

        public static Option CreateOutputFileOption()
        {
            var option = new Option<FileInfo?>(
                "--output-file") {
                Description = "Output the captured traffic to an archive file",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = result => new FileInfo(result.Tokens.First().Value)
            };

            option.Aliases.Add("-o");

            return option;
        }

        public static Option CreateDumpToFolderOption()
        {
            var option = new Option<DirectoryInfo>(
                "--dump-folder") {
                Description = "Output the captured traffic to folder"
            };

            option.Aliases.Add("-d");

            return option;
        }

        public static Option CreateSystemProxyOption()
        {
            var option = new Option<bool>(
                "--system-proxy") {
                Description = "Try to register fluxzy as system proxy when started",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-sp");

            return option;
        }

        public static Option CreateTcpDumpOption()
        {
            var option = new Option<bool>(
                "--include-dump") {
                Description = "Include tcp dumps on captured output",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-c");

            return option;
        }

        public static Option CreateSkipSslOption()
        {
            var option = new Option<bool>(
                "--skip-ssl-decryption") {
                Description = "Disable ssl traffic decryption",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-ss");

            return option;
        }

        public static Option CreateListenLocalhost()
        {
            var option = new Option<bool>(
                "--llo") {
                Description = "Listen on localhost address with default port. Same as -l 127.0.0.1/44344",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            return option;
        }

        public static Option CreateListToAllInterfaces()
        {
            var option = new Option<bool>(
                "--lany") {
                Description = "Listen on all interfaces with default port (44344)",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            return option;
        }

        public static Option CreateBouncyCastleOption()
        {
            var option = new Option<bool>(
                "--bouncy-castle") {
                Description = "Use Bouncy Castle as SSL/TLS provider",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-b");

            return option;
        }

        public static Option CreateSkipCertInstallOption()
        {
            var option = new Option<bool>(
                "--install-cert") {
                Description = "Install root CA in current cert store if absent (require higher privilege)",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-i");

            return option;
        }

        public static Option CreateSkipRemoteCertificateValidation()
        {
            var option = new Option<bool>(
                "--insecure") {
                Description = "Skip remote certificate validation globally. Use `SkipRemoteCertificateValidationAction` for specific host only",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-k");

            return option;
        }

        public static Option<ProxyMode> CreateReverseProxyMode()
        {
            var possibleValues = string.Join(", ",
                Enum.GetNames(typeof(ProxyMode)));

            var option = new Option<ProxyMode>(
                "--mode") {
                Description = "Set proxy mode",
                Arity = ArgumentArity.ExactlyOne,
                AllowMultipleArgumentsPerToken = false,
                DefaultValueFactory = _ => ProxyMode.Regular,
                CustomParser = result => {
                    var value = result.Tokens.FirstOrDefault()?.Value;

                    if (value == null) {
                        result.AddError("Invalid proxy mode value");

                        return default;
                    }

                    if (!Enum.TryParse<ProxyMode>(value, true, out var finalResult)
                        || (int) finalResult == 0) {
                        result.AddError($"Invalid proxy mode value. Possible values are: {possibleValues}");

                        return default;
                    }

                    return finalResult;
                }
            };

            return option;
        }

        public static Option<int?> CreateReverseProxyModePortOption()
        {
            var option = new Option<int?>(
                "--mode-reverse-port") {
                Description = "Set the remote authority port when --mode ReverseSecure or --mode ReversePlain is set",
                Arity = ArgumentArity.ExactlyOne
            };

            return option;
        }

        public static Option CreateNoCertCacheOption()
        {
            var option = new Option<bool>(
                "--no-cert-cache") {
                Description = "Don't cache generated certificate on file system",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            return option;
        }

        public static Option CreateUaParsingOption()
        {
            var option = new Option<bool>(
                "--parse-ua") {
                Description = "Parse user agent",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            return option;
        }

        public static Option CreateUser502Option()
        {
            var option = new Option<bool>(
                "--use-502") {
                Description = "Use 502 status code for upstream error instead of 528.",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            return option;
        }

        public static Option CreateOutOfProcCaptureOption()
        {
            var option = new Option<bool>(
                "--external-capture") {
                Description = "Indicates that the raw capture will be done by an external process",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            return option;
        }

        public static Option CreateCertificateFileOption()
        {
            var option = new Option<FileInfo>(
                "--cert-file") {
                Description = "Substitute the default CA certificate with a compatible PKCS#12 (p12, pfx) root CA certificate for SSL decryption",
                Arity = ArgumentArity.ExactlyOne
            };

            return option;
        }

        public static Option CreateCertificatePasswordOption()
        {
            var option = new Option<string>(
                "--cert-password") {
                Description = "Set the password of certfile if any",
                Arity = ArgumentArity.ExactlyOne
            };

            return option;
        }

        public static Option CreateProxyBuffer()
        {
            var option = new Option<int?>(
                "--request-buffer") {
                Description = "Set the default request buffer",
                Arity = ArgumentArity.ExactlyOne
            };

            return option;
        }

        public static Option CreateMaxConnectionPerHost()
        {
            var option = new Option<int>(
                "--max-upstream-connection") {
                Description = "Maximum connection per upstream host",
                Arity = ArgumentArity.ExactlyOne,
                DefaultValueFactory = _ => FluxzySharedSetting.MaxConnectionPerHost
            };

            return option;
        }

        public static Option CreateProxyAuthenticationOption()
        {
            var option = new Option<NetworkCredential?>(
                "--proxy-auth-basic") {
                Description = "Require a basic authentication. Username and password shall be provided in this format: username:password." +
                    " Values can be provided in a percent encoded format.",
                Arity = ArgumentArity.ExactlyOne,
                CustomParser = result => {
                    var value = result.Tokens.FirstOrDefault()?.Value;

                    if (value == null) {
                        result.AddError("Credentials must be provided.");

                        return default;
                    }

                    var arrayCredentials = value.Split(':');

                    if (arrayCredentials.Length == 1) {
                        result.AddError("Username and password must be separated by with column.");

                        return default;
                    }

                    if (arrayCredentials.Length > 2) {
                        result.AddError(
                            "Provided credentials contains multiple columns. Use %3A for column in username or password.");

                        return default;
                    }

                    var username = WebUtility.UrlDecode(arrayCredentials[0]);
                    var password = WebUtility.UrlDecode(arrayCredentials[1]);

                    return new NetworkCredential(username, password);
                }
            };

            return option;
        }

        public static Option CreateRuleFileOption()
        {
            var option = new Option<FileInfo>(
                "--rule-file") {
                Description = "Use a fluxzy rule file. See more at : https://www.fluxzy.io/resources/documentation/the-rule-file",
                Arity = ArgumentArity.ExactlyOne
            };

            option.Aliases.Add("-r");

            return option;
        }

        public static Option CreateCounterOption()
        {
            var option = new Option<int?>(
                "--max-capture-count") {
                Description = "Exit after a specified count of exchanges",
                Arity = ArgumentArity.ExactlyOne
            };

            option.Aliases.Add("-n");

            return option;
        }

        public static Option CreateRuleStdinOption()
        {
            var option = new Option<bool>(
                "--rule-stdin") {
                Description = "Read rule from stdin",
                Arity = ArgumentArity.Zero
            };

            option.Aliases.Add("-R");

            return option;
        }

        public static Option CreateEnableTracingOption()
        {
            var option = new Option<bool>(
                "--trace") {
                Description = "Output trace on stdout",
                Arity = ArgumentArity.Zero,
                DefaultValueFactory = _ => false
            };

            option.Aliases.Add("-t");

            return option;
        }
    }
}
