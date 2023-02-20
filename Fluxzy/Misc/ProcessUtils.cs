// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Diagnostics;
using System.IO;
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

        public static async Task<ProcessRunResult> QuickRunAsync(string commandName, string args, Stream ? stdinStream = null)
        {
            // Run process and return process run result 
            
            var processStartInfo = new ProcessStartInfo(commandName, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = stdinStream != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var process = Process.Start(processStartInfo);
            
            if (process == null)
                throw new InvalidOperationException("Unable to start process " + commandName + " " + args);

            Task? copyTask = null; 
            
            if (stdinStream != null) {
                // Copy stdinstream to process stdin
                
                copyTask = 
                            stdinStream.CopyToAsync(process.StandardInput.BaseStream)
                                       .ContinueWith(t => process.StandardInput.Dispose());
            }
            
            var standardOutputReading =  process.StandardOutput.ReadToEndAsync();
            var standardErrorReading = process.StandardError.ReadToEndAsync();

            if (copyTask != null)
                await copyTask;
            
            var standardOutput = await standardOutputReading;
            var standardError = await standardErrorReading;
            

            await process.WaitForExitAsync();
            
            return new ProcessRunResult(standardError, standardOutput, process.ExitCode);
        }
        
        public static ProcessRunResult QuickRun(string commandName, string args, Stream ? stdinStream = null)
        {
            // Run process and return process run result 
            
            var processStartInfo = new ProcessStartInfo(commandName, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = stdinStream != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var process = Process.Start(processStartInfo);
            
            if (process == null)
                throw new InvalidOperationException("Unable to start process " + commandName + " " + args);

            if (stdinStream != null) {
                // Copy stdinstream to process stdin
                stdinStream.CopyTo(process.StandardInput.BaseStream);
                process.StandardInput.BaseStream.Dispose();
            }
            
            
            
            var standardOutput =  process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            

            process.WaitForExit();
            
            return new ProcessRunResult(standardError, standardOutput, process.ExitCode);
        }

        public static Task<ProcessRunResult> QuickRunAsync(string fullCommand, Stream ? stdinStream = null)
        {
            var commandTab = fullCommand.Split(' ');

            if (commandTab.Length == 1)
                return QuickRunAsync(fullCommand, string.Empty); 
            
            
            var commandName = fullCommand.Split(' ')[0];
            var args = fullCommand.Substring(commandName.Length + 1);

            return QuickRunAsync(commandName, args,stdinStream); 
        }
        
        public static ProcessRunResult QuickRun(string fullCommand, Stream ? stdinStream = null)
        {
            var commandTab = fullCommand.Split(' ');

            if (commandTab.Length == 1)
                return QuickRun(fullCommand, string.Empty); 
            
            
            var commandName = fullCommand.Split(' ')[0];
            var args = fullCommand.Substring(commandName.Length + 1);

            return QuickRun(commandName, args,stdinStream); 
        }

        public static bool IsCommandAvailable(string commandName)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = commandName,
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try {

                process.Start();
                process.WaitForExit(100);
            }
            catch {
                // ignore further error 
                return false; 
            }

            return true; 
        }
    }

    internal static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (process.HasExited) 
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<object?>();
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(() => tcs.SetCanceled());

            return process.HasExited ? Task.CompletedTask : tcs.Task;
        }
    }


    public class ProcessRunResult
    {
        public ProcessRunResult(string? standardErrorMessage, string? standardOutputMessage, int? exitCode)
        {
            StandardErrorMessage = standardErrorMessage;
            StandardOutputMessage = standardOutputMessage;
            ExitCode = exitCode;
        }
        
        public int?  ExitCode { get;  }

        public string ? StandardErrorMessage { get;  }
        
        public string ? StandardOutputMessage { get;  }
        
    }
}
