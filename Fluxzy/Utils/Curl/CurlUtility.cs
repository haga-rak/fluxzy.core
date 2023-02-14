// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Diagnostics;

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

        public static bool RunCurl(string args, out string stdout, out string stdErr)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "curl",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            
            process.Start();

            stdout = process.StandardOutput.ReadToEnd();
            stdErr = process.StandardError.ReadToEnd();
            
            process.WaitForExit();

            return process.ExitCode == 0;
        }
    }
}