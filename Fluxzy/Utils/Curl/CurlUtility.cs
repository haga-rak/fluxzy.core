// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Diagnostics;
using System.IO;

namespace Fluxzy.Utils.Curl
{
    public static class CurlUtility
    {
        public static bool CheckCurlIsInstalled()
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

        public static bool RunCurl(string args, string? workDirectory)
        {
            var process = new Process
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
            process.WaitForExit();

            return process.ExitCode == 0;
        }
    }
}