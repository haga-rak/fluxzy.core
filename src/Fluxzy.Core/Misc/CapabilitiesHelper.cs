// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    internal static class CapabilityHelper
    {
        public static async Task<HashSet<string>?> GetCapabilities(int processId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null; 
            
            var runResult = await ProcessUtils.QuickRunAsync($"getpcaps {processId} --legacy");
            
            if (runResult.ExitCode != 0) {
                // error running getpcaps: assuming no capabilities
                return null;
            }

            var rawOutput = runResult.StandardOutputMessage;

            if (!TryParseOutput(processId, rawOutput, out var result))
                return null;

            return result; 
        }

        public static bool TryParseOutput(int processId, string? rawOutput, out HashSet<string> result)
        {
            result = null!; 
            
            if (string.IsNullOrWhiteSpace(rawOutput))
                return false; 

            var prefix = $"{processId}: ";

            if (!rawOutput.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            result = rawOutput.Substring(prefix.Length)
                              .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim(' ', '\r', '\n'))
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return true;
        }
    }
}
