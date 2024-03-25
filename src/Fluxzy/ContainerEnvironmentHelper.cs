// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Cli
{
    internal static class ContainerEnvironmentHelper
    {
        public static bool IsInContainer(string[] args)
        {
            return args.Any(arg => arg.Equals("--container",
                StringComparison.OrdinalIgnoreCase));
        }

        public static string[] CreateArgsFromEnvironment(EnvironmentProvider environmentProvider)
        {
            var finalArgs = new List<string>();

            var listenAddress = environmentProvider.GetEnvironmentVariable("FLUXZY_ADDRESS")
                                ?? "0.0.0.0";

            var listenPort = environmentProvider.GetInt32EnvironmentVariable("FLUXZY_PORT")
                             ?? 44344;

            finalArgs.Add("--listen-interface");
            finalArgs.Add($"{listenAddress}/{listenPort}");

            if (environmentProvider.EnvironmentVariableActive("FLUXZY_ENABLE_DUMP_FOLDER")) {
                var dumpPath = "/var/fluxzy/dump";
                finalArgs.Add("--dump-folder");
                finalArgs.Add(dumpPath);
            }

            if (environmentProvider.EnvironmentVariableActive("FLUXZY_ENABLE_OUTPUT_FILE")) {
                var outputPath = "/var/fluxzy/out.fxzy";
                finalArgs.Add("--output-file");
                finalArgs.Add(outputPath);
            }

            if (environmentProvider.EnvironmentVariableActive("FLUXZY_ENABLE_PCAP")) {
                finalArgs.Add("--include-dump");
            }

            if (environmentProvider.EnvironmentVariableActive("FLUXZY_USE_BOUNCY_CASTLE")) {
                finalArgs.Add("--bouncy-castle");
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_CUSTOM_CA_PATH",
                    out var certPath)) {
                finalArgs.Add("--cert-file");
                finalArgs.Add(certPath);
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_CUSTOM_CA_PASSWORD",
                    out var certPassword)) {
                finalArgs.Add("--cert-password");
                finalArgs.Add(certPassword);
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_MODE", out var fluxzyMode)) {
                // Regular|ReversePlain|ReverseSecure

                var possibleValues = new HashSet<string> {
                    "Regular", "ReversePlain", "ReverseSecure"
                };

                if (possibleValues.Contains(fluxzyMode)) {
                    finalArgs.Add("--mode");
                    finalArgs.Add(fluxzyMode);
                }
                else {
                    throw new InvalidOperationException("Invalid mode value for variable FLUXZY_MODE");
                }
            }

            if (environmentProvider.TryGetInt32EnvironmentVariable("FLUXZY_MODE_REVERSE_PORT",
                    out var modeReversePort)) {
                finalArgs.Add("--mode-reverse-port");
                finalArgs.Add($"{modeReversePort}");
            }

            if (environmentProvider.TryGetEnvironmentVariable("FLUXZY_EXTRA_ARGS", out var extraArgs)) {
                finalArgs.AddRange(ArgsHelper.SplitArgs(extraArgs));
            }

            return finalArgs.ToArray();
        }
    }

    internal abstract class EnvironmentProvider
    {
        public virtual bool EnvironmentVariableActive(string name)
        {
            var variableValue = GetEnvironmentVariable(name);

            if (variableValue == null) {
                return false;
            }

            return variableValue.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || variableValue.Equals("1");
        }

        public abstract string? GetEnvironmentVariable(string variable);

        public bool TryGetEnvironmentVariable(string variable, out string value)
        {
            value = GetEnvironmentVariable(variable)!;

            return value != null!;
        }

        public bool TryGetInt32EnvironmentVariable(string variable, out int value)
        {
            var rawValue = GetEnvironmentVariable(variable);

            if (rawValue != null && int.TryParse(rawValue, out value)) {
                return true;
            }

            value = 0;

            return false;
        }

        public virtual int? GetInt32EnvironmentVariable(string variable)
        {
            var rawValue = GetEnvironmentVariable(variable);

            if (rawValue != null && int.TryParse(rawValue, out var value)) {
                return value;
            }

            return null;
        }
    }

    internal class SystemEnvironmentProvider : EnvironmentProvider
    {
        public override string? GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
    }

    internal class DictionaryEnvironmentProvider : EnvironmentProvider
    {
        private readonly Dictionary<string, string> _environmentVariables;

        public DictionaryEnvironmentProvider(Dictionary<string, string> environmentVariables)
        {
            _environmentVariables = environmentVariables;
        }

        public override string? GetEnvironmentVariable(string variable)
        {
            return _environmentVariables.TryGetValue(variable, out var value) ? value : null;
        }
    }
}
