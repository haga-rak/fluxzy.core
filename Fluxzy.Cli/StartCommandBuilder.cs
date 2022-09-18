// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace Fluxzy.Cli
{
    public class StartCommandBuilder
    {
        public static Command Build()
        {
            var command = new Command("start", "Start a capturing session");

            command.AddOption(CreateListenInterfaceOption()); 
            command.AddOption(CreateOutputFileOption()); 
            command.AddOption(CreateArchivingPolicyOption()); 
            command.AddOption(CreateSystemProxyOption()); 
            command.AddOption(CreateTcpDumpOption()); 
            command.AddOption(CreateSkipSslOption()); 

            return command; 
        }

        private static Option CreateListenInterfaceOption()
        {
            var listenInterfaceOption = new Option<List<IPEndPoint>>(
                name: "--listen-interface",
                description:
                "Set up the binding addresses. " +
                "Default value is \"127.0.0.1/44344\" which will listen to localhost on port 44344. " +
                "0.0.0.0 to listen on all interface with default port." +
                "Accept multiple values.",
                isDefault: true,
                parseArgument: result =>
                {
                    var listResult = new List<IPEndPoint>(); 

                    foreach (var token in result.Tokens)
                    {
                        var tab = token.Value.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                        if (tab.Length == 1)
                        {
                            if (!IPAddress.TryParse(tab.First(), out var ipAddress))
                            {
                                result.ErrorMessage = $"Invalid ip address {tab.First()}"; 
                                return null; 
                            }

                            listResult.Add(new IPEndPoint(ipAddress, 44344));
                        }
                        else
                        {
                            if (!IPAddress.TryParse(tab.First(), out var ipAddress))
                            {
                                result.ErrorMessage = $"Invalid ip address {tab.First()}";
                                return null;
                            }

                            var portString = string.Join("", tab.Skip(1));
                            if (!int.TryParse(portString, out var port))
                            {
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
            listenInterfaceOption.SetDefaultValue(new IPEndPoint(IPAddress.Loopback,  44344));
            listenInterfaceOption.Arity = ArgumentArity.ZeroOrMore;

            return listenInterfaceOption; 
        }

        private static Option CreateOutputFileOption()
        {
            var option = new Option<FileInfo>(
                name: "--output-file",
                description: "Output the captured traffic to file");

            option.AddAlias("-o");
            option.Arity = ArgumentArity.ZeroOrOne;

            return option;
        }

        private static Option CreateArchivingPolicyOption()
        {
            var option = new Option<ArchivingPolicy>(
                name: "--dump-folder",
                isDefault: false,
                description: "Output the captured traffic to folder",
                parseArgument: result => ArchivingPolicy.CreateFromDirectory(result.Tokens.First().Value));

            option.AddAlias("-d");
            option.SetDefaultValue(ArchivingPolicy.None);

            return option; 
        }

        private static Option CreateSystemProxyOption()
        {
            var option = new Option<bool>(
                name: "--system-proxy",
                description: "Try to register fluxzy as system proxy when started");

            option.AddAlias("-sp");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option; 
        }

        private static Option CreateTcpDumpOption()
        {
            var option = new Option<bool>(
                name: "--include-dump",
                description: "Include tcp dumps on captured output");

            option.AddAlias("-c");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option; 
        }

        private static Option CreateSkipSslOption()
        {
            var option = new Option<bool>(
                name: "--skip-ssl-decryption",
                description: "Disable ssl traffic decryption");

            option.AddAlias("-sk");
            option.SetDefaultValue(false);
            option.Arity = ArgumentArity.Zero;

            return option; 
        }
    }
}