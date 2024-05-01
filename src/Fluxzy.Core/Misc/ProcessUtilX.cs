// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    internal static class ProcessUtilX
    {
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
    }
}
