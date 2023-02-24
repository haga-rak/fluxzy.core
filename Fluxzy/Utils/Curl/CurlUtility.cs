// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.Utils.Curl
{
    public static class CurlUtility
    {
        public static bool IsCurlInstalled()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "curl",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        public static async Task<bool> RunCurl(string args, string? workDirectory)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "curl",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                }
            };

            if (workDirectory != null)
                process.StartInfo.WorkingDirectory = new DirectoryInfo(workDirectory).FullName; 
            
            process.Start();
            
            await process.WaitForExitAsync();

            return process.ExitCode == 0 
                   || process.ExitCode == 23; //curl exit 23 when  stdout close early even the command succeed
        }
    }
}