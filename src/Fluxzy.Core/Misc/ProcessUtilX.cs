// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    internal static class ProcessUtilX
    {
        private static readonly string[] AskPassBinaries = new[] { "ssh-askpass", "ksshaskpass", "lxqt-sudo" };
        
        
        private static readonly HashSet<string> RequiredCapabilities = new HashSet<string>(
            new[] { "cap_net_raw", "cap_net_admin" }, 
            StringComparer.OrdinalIgnoreCase);

        public static async Task<bool> CanElevated()
        {
            var checkStartInfo = new ProcessStartInfo("sudo", "-n -v")
            {
                UseShellExecute = false,
            };

            var checkStart = Process.Start(checkStartInfo)!;

            await checkStart.WaitForExitAsync().ConfigureAwait(false);
            return checkStart.ExitCode == 0;
        }

        public static async Task<bool> HasCaptureCapabilities()
        {
            var availableCapabilities = await CapabilityHelper.GetCapabilities(Process.GetCurrentProcess().Id);
            
            if (availableCapabilities == null)
                return false;
            
            return availableCapabilities.IsSupersetOf(RequiredCapabilities);
        }

        public static async Task<string?> GetExecutablePath(string executableName)
        {
            var processRunResult = await ProcessUtils.QuickRunAsync("which", executableName, null);
            
            if (processRunResult.ExitCode != 0)
                return null;
            
            return processRunResult.StandardOutputMessage?.Trim('\r', '\n');
        }
        
        public static async Task<Process> RunElevatedSudoALinux(
            string commandName, string[] args, bool redirectStdOut,
            string askPasswordPrompt, bool redirectStandardError = false)
        {
            var askPassBinaries = AskPassBinaries;

            if (Environment.GetEnvironmentVariable("FLUXZY_ASKPASS") is { } providedAskPassVariables) {
                askPassBinaries = new[] { providedAskPassVariables }.Concat(askPassBinaries).ToArray();
            }

            var tasks = askPassBinaries
                .Select(binary => GetExecutablePath(binary));
            
            var results = await Task.WhenAll(tasks);
            var result = results.FirstOrDefault(x => x != null) ?? 
                         throw new Exception(
                             $"No askpass binary found. Must install one of the following:" +
                             $" {string.Join(", ", askPassBinaries)}");
            
            var execCommandName = "sudo";
            var preArgs = new List<string>() { "-A", "-E", "-p", $"{askPasswordPrompt}", commandName };

            var startInfo = new ProcessStartInfo() {
                FileName = execCommandName,
                RedirectStandardOutput = redirectStdOut,
                RedirectStandardError = redirectStandardError,
                UseShellExecute = false,
            }; 
            
            startInfo.EnvironmentVariables["SUDO_ASKPASS"] = result;
            foreach (var arg in preArgs.Concat(args))
                startInfo.ArgumentList.Add(arg);
            
            var process = Process.Start(startInfo);

            return process!;
        }
    }
}
