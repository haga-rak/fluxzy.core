// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
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
        public static async Task<bool> AcquireCapabilitiesLinux(string executablePath)
        {
            if (!File.Exists(executablePath)) {
                executablePath = (await ProcessUtilX.GetExecutablePath(executablePath))
                    ?? throw new InvalidOperationException("Executable not found");
            }
            
            if (await ProcessUtilX.CanElevated())
                return true; // Already root  - no need to set capabilities
            
            if (!ProcessUtils.IsCommandAvailable("setcap"))
                return false; 
            
            var process = await ProcessUtilX.RunElevatedSudoALinux("setcap", 
                new []{ "cap_net_raw,cap_net_admin=eip", executablePath},
                false, "Please enter your password to set the required capabilities");
            
            await process!.WaitForExitAsync();
            return process.ExitCode == 0; 
        }
    }
}
