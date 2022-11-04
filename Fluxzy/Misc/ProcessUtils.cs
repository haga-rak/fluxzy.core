// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    public static class ProcessUtils
    {
        public static string? RunAndExpectZero(string commandLine, string args)
        {
            var processStartInfo = new ProcessStartInfo(commandLine, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            
            var process = Process.Start(processStartInfo);

            if (process == null)
                return null;

            Span<char> charBuffer = stackalloc char[1024];
            var stringBuilder = new StringBuilder();
            
            int read;
            while ((read = process.StandardOutput.Read(charBuffer)) > 0)
            {
                stringBuilder.Append(charBuffer.Slice(0, read)); 
            }

            process.WaitForExit();

            return process.ExitCode == 0  ? stringBuilder.ToString() : null;
        }
    }


    internal static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (process.HasExited) 
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(() => tcs.SetCanceled());

            return process.HasExited ? Task.CompletedTask : tcs.Task;
        }
    }
}
