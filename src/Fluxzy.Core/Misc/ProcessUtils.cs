// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    internal static class ProcessUtils
    {
        public static string? RunAndExpectZero(string commandLine, string args)
        {
            var processStartInfo = new ProcessStartInfo(commandLine, args) {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(processStartInfo);

            if (process == null) {
                return null;
            }

            Span<char> charBuffer = stackalloc char[1024];
            var stringBuilder = new StringBuilder();

            int read;

            while ((read = process.StandardOutput.Read(charBuffer)) > 0) {
                stringBuilder.Append(charBuffer.Slice(0, read));
            }

            process.WaitForExit();

            return process.ExitCode == 0 ? stringBuilder.ToString() : null;
        }

        public static async Task<ProcessRunResult> QuickRunAsync(
            string commandName, string args, Stream? stdinStream = null, bool throwOnFail = false)
        {
            // Run process and return process run result 

            var processStartInfo = new ProcessStartInfo(commandName, args) {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = stdinStream != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(processStartInfo);

            if (process == null) {
                throw new InvalidOperationException("Unable to start process " + commandName + " " + args);
            }

            Task? copyTask = null;

            if (stdinStream != null) {
                // Copy stdinstream to process stdin

                copyTask =
                    stdinStream.CopyToAsync(process.StandardInput.BaseStream)
                               .ContinueWith(t => process.StandardInput.Dispose());
            }

            var standardOutputReading = process.StandardOutput.ReadToEndAsync();
            var standardErrorReading = process.StandardError.ReadToEndAsync();

            if (copyTask != null) {
                await copyTask.ConfigureAwait(false);
            }

            var standardOutput = await standardOutputReading.ConfigureAwait(false);
            var standardError = await standardErrorReading.ConfigureAwait(false);

            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode != 0 && throwOnFail) {
                throw new InvalidOperationException(
                    $"Process {commandName} {args} exited" +
                    $" with code {process.ExitCode} {standardOutput} {standardError}");
            }

            return new ProcessRunResult(standardError, standardOutput, process.ExitCode);
        }

        public static ProcessRunResult QuickRun(
            string commandName, string args, Stream? stdInStream = null, bool throwOnFail = false)
        {
            // Run process and return process run result 

            var processStartInfo = new ProcessStartInfo(commandName, args) {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = stdInStream != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(processStartInfo);

            if (process == null) {
                throw new InvalidOperationException("Unable to start process " + commandName + " " + args);
            }

            if (stdInStream != null) {
                // Copy stdinstream to process stdin
                stdInStream.CopyTo(process.StandardInput.BaseStream);
                process.StandardInput.BaseStream.Dispose();
            }

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0 && throwOnFail) {
                throw new InvalidOperationException(
                    $"Process {commandName} {args} exited" +
                    $" with code {process.ExitCode} {standardOutput} {standardError}");
            }

            return new ProcessRunResult(standardError, standardOutput, process.ExitCode);
        }

        public static Task<ProcessRunResult> QuickRunAsync(
            string fullCommand, Stream? stdInStream = null, bool throwOnFail = false)
        {
            var commandTab = fullCommand.Split(' ');

            if (commandTab.Length == 1) {
                return QuickRunAsync(fullCommand, string.Empty, throwOnFail: throwOnFail);
            }

            var commandName = fullCommand.Split(' ')[0];
            var args = fullCommand.Substring(commandName.Length + 1);

            return QuickRunAsync(commandName, args, stdInStream, throwOnFail);
        }

        public static ProcessRunResult QuickRun(
            string fullCommand, Stream? stdInStream = null, bool throwOnFail = false)
        {
            var commandTab = fullCommand.Split(' ');

            if (commandTab.Length == 1) {
                return QuickRun(fullCommand, string.Empty, throwOnFail: throwOnFail);
            }

            var commandName = fullCommand.Split(' ')[0];
            var args = fullCommand.Substring(commandName.Length + 1);

            return QuickRun(commandName, args, stdInStream, throwOnFail);
        }

        public static bool IsCommandAvailable(string commandName)
        {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = commandName,
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
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

        public static async Task<Process?> RunElevatedAsync(
            string commandName, string[] args, bool redirectStdOut,
            string askPasswordPrompt, bool redirectStandardError = false)
        {
            var fullArgs = string.Join(" ", args.Select(s => s.EscapeSegment()));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // We use run as it is 
                var winProcess = Process.Start(new ProcessStartInfo(commandName, fullArgs) {
                    UseShellExecute = false,
                    Verb = "runas",
                    RedirectStandardOutput = redirectStdOut,
                    RedirectStandardInput = redirectStdOut,
                    CreateNoWindow = true
                });

                return winProcess;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                if (Environment.GetEnvironmentVariable("FluxzyDesktopVersion") != null
                    || string.Equals(Environment.GetEnvironmentVariable("FluxzyGraphicalPrivilegePrompt"), "TRUE",
                        StringComparison.OrdinalIgnoreCase)) {
                    var acquired = await ProcessUtilsOsx.OsxTryAcquireElevation(askPasswordPrompt)
                                                        .ConfigureAwait(false);

                    if (!acquired) {
                        return null;
                    }
                }

                var osXProcess = Process.Start(new ProcessStartInfo("sudo", $"-n \"{commandName}\" {fullArgs}") {
                    UseShellExecute = false,
                    Verb = "runas",
                    RedirectStandardOutput = redirectStdOut,
                    RedirectStandardInput = redirectStdOut,
                    RedirectStandardError = redirectStandardError
                });

                return osXProcess;
            }

            // For other OS we use pkexec

            var canElevated = await ProcessUtilX.HasCaptureCapabilities() || await ProcessUtilX.CanElevated();
            var execCommandName = canElevated ? commandName : "pkexec";
            var execArgs = canElevated ? fullArgs : $"\"{commandName}\" {fullArgs}";

            var process = Process.Start(new ProcessStartInfo(execCommandName, execArgs) {
                UseShellExecute = false,
                Verb = "runas",
                RedirectStandardOutput = redirectStdOut,
                RedirectStandardInput = redirectStdOut,
                RedirectStandardError = redirectStandardError
            });

            return process;
        }

        public static Process? RunElevated(
            string commandName, string[] args, bool redirectStdOut,
            string askPasswordPrompt)
        {
            return
                Task.Run(() => RunElevatedAsync(commandName, args, redirectStdOut, askPasswordPrompt)).GetAwaiter()
                    .GetResult();
        }
    }

    internal static class ProcessExtensions
    {
        public static Task<int?> WaitForExitAsync(
            this Process process,
            CancellationToken cancellationToken = default)
        {
            if (process.HasExited) {
                return Task.FromResult<int?>(process.ExitCode);
            }

            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => tcs.TrySetResult(null);

            if (cancellationToken != default) {
                cancellationToken.Register(() => tcs.SetCanceled());
            }

            return Task.FromResult<int?>(process.HasExited ? process.ExitCode : null);
        }

        public static string EscapeSegment(this string commandLineSegment)
        {
            var mustMeEscapedChars = "\"'|<> "; // Char that must be escaped or quoted for both CMD and Bash

            var mustBeEscapedAndQuoted = commandLineSegment.Intersect(mustMeEscapedChars).Any();

            if (mustBeEscapedAndQuoted) {
                commandLineSegment = "\"" + commandLineSegment.SanitizeQuote() + "\"";
            }

            return commandLineSegment;
        }

        internal static string SanitizeQuote(this string str)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return str.Replace("\"", "\"\"");
            }

            return str.Replace("'", "'\\''");
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

        public int? ExitCode { get; }

        public string? StandardErrorMessage { get; }

        public string? StandardOutputMessage { get; }
    }
}
