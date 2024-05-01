// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Diagnostics;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    internal static class ProcessUtilX
    {
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
    }
}
