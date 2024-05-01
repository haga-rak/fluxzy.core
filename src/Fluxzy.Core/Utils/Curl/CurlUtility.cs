// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.Utils.Curl
{
    public static class CurlUtility
    {
        public static async Task<bool> RunCurl(string args, string? workDirectory)
        {
            using var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "curl",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (workDirectory != null)
                process.StartInfo.WorkingDirectory = new DirectoryInfo(workDirectory).FullName;

            process.Start();

            var stdOutPromise = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
            var stdErrPromise = process.StandardError.BaseStream.CopyToAsync(Stream.Null);

            await process.WaitForExitAsync().ConfigureAwait(false);
            
            await Task.WhenAll(stdOutPromise, stdErrPromise);

            return process.ExitCode == 0
                   || process.ExitCode == 23; //curl exit 23 when  stdout close early even the command succeed
        }
    }
}
