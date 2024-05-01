// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.Tests
{
    public static class Utility
    {
        /// <summary>
        ///  Make an executable have the required capabilities
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> AcquireCapabilities(string executablePath)
        {
            if (await ProcessUtilX.CanElevated())
                return true; // Already root  - no need to set capabilities
            
            if (!ProcessUtils.IsCommandAvailable("setcap"))
                return false; 
            
            if (!ProcessUtils.IsCommandAvailable("pkexec"))
                return false; 

            var fullCommand = $"setcap cap_net_raw,cap_net_admin=eip \"{executablePath}\""; 
            
            var process = await ProcessUtils.RunElevatedAsync("pkexec", 
                new []{ "setcap", "cap_net_raw,cap_net_admin=eip", executablePath},
                false, "Please enter your password to set the required capabilities");
            
            await process!.WaitForExitAsync();

            return process.ExitCode == 0; 
        }
    }
}
