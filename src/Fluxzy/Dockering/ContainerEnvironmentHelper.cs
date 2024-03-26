// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Cli.Dockering
{
    internal static class ContainerEnvironmentHelper
    {
        public static bool IsInContainer(EnvironmentProvider environmentProvider)
        {
            return environmentProvider.EnvironmentVariableActive("FLUXZY_CONTAINERIZED");
        }

        public static string[] CreateArgsFromEnvironment(string[] originalArgs, EnvironmentProvider environmentProvider)
        {
            var finalArgs = new List<string>();

            // finalArgs.AddRange(originalArgs.Where(a => a != "start" && a != "--container"));
            finalArgs.AddRange(originalArgs);

            if (originalArgs.FirstOrDefault() == "start")
            {
                var listenAddress = environmentProvider.GetEnvironmentVariable("FLUXZY_ADDRESS")
                                    ?? "0.0.0.0";

                var listenPort = environmentProvider.GetInt32EnvironmentVariable("FLUXZY_PORT")
                                 ?? 44344;

                finalArgs.Add("--listen-interface");
                finalArgs.Add($"{listenAddress}/{listenPort}");

                if (environmentProvider.EnvironmentVariableActive("FLUXZY_ENABLE_DUMP_FOLDER"))
                {
                    var dumpPath = "/var/fluxzy/dump";
                    finalArgs.Add("--dump-folder");
                    finalArgs.Add(dumpPath);
                }
            }

            if (environmentProvider.EnvironmentVariableActive("FLUXZY_ENABLE_OUTPUT_FILE"))
            {
                var outputPath = "/var/fluxzy/out.fxzy";
                finalArgs.Add("--output-file");
                finalArgs.Add(outputPath);
            }

            if (environmentProvider.EnvironmentVariableActive("FLUXZY_ENABLE_PCAP"))
            {
                finalArgs.Add("--include-dump");
            }

            if (environmentProvider.EnvironmentVariableActive("FLUXZY_USE_BOUNCY_CASTLE"))
            {
                finalArgs.Add("--bouncy-castle");
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_CUSTOM_CA_PATH",
                    out var certPath))
            {
                finalArgs.Add("--cert-file");
                finalArgs.Add(certPath);
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_CUSTOM_CA_PASSWORD",
                    out var certPassword))
            {
                finalArgs.Add("--cert-password");
                finalArgs.Add(certPassword);
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_MODE", out var fluxzyMode))
            {
                // Regular|ReversePlain|ReverseSecure

                var possibleValues = new HashSet<string> {
                    "Regular", "ReversePlain", "ReverseSecure"
                };

                if (possibleValues.Contains(fluxzyMode))
                {
                    finalArgs.Add("--mode");
                    finalArgs.Add(fluxzyMode);
                }
                else
                {
                    throw new InvalidOperationException("Invalid mode value for variable FLUXZY_MODE");
                }
            }

            if (environmentProvider.TryGetInt32EnvironmentVariable("FLUXZY_MODE_REVERSE_PORT",
                    out var modeReversePort))
            {
                finalArgs.Add("--mode-reverse-port");
                finalArgs.Add($"{modeReversePort}");
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_EXTRA_ARGS", out var extraArgs))
            {
                finalArgs.AddRange(ArgsHelper.SplitArgs(extraArgs));
            }

            return finalArgs.ToArray();
        }
    }
}
