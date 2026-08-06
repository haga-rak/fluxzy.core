// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Misc
{
    internal static class ProcessUtilsOsx
    {
        /// <summary>
        /// Launches commandName as root through the system authorization dialog, keeping live
        /// stdin/stdout on the returned process. The password is collected by macOS, never by
        /// this process.
        /// </summary>
        internal static Process? StartElevatedStreamed(
            string commandName, string[] args, bool redirectStandardError)
        {
            // security(1) forwards this process's stdin to the elevated child, but never reads
            // back the AuthorizationExecuteWithPrivileges pipe that carries the child's stdout;
            // only the child's stderr flows back. Swap stdout/stderr inside the elevated shell
            // and swap them back outside, so the child's stdout lands on StandardOutput.
            var elevated = new[] {
                "/usr/bin/security", "-q", "execute-with-privileges",
                "/bin/sh", "-c", "exec \"$0\" \"$@\" 1>&2", commandName
            }.Concat(args);

            var startInfo = new ProcessStartInfo("/bin/sh") {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = redirectStandardError
            };

            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add($"exec {string.Join(" ", elevated.Select(QuoteForShell))} 2>&1");

            return Process.Start(startInfo);
        }

        /// <summary>
        /// Runs a one-shot command as root through the system authorization dialog. The returned
        /// process exits when the command completes and reflects its failure in the exit code.
        /// </summary>
        internal static Process? StartElevatedOneShot(string commandName, string[] args, string prompt)
        {
            var command = string.Join(" ",
                new[] { commandName }.Concat(args).Select(QuoteForShell));

            var script = $"do shell script {QuoteForAppleScript(command)}";

            if (!string.IsNullOrWhiteSpace(prompt)) {
                script += $" with prompt {QuoteForAppleScript(prompt)}";
            }

            script += " with administrator privileges";

            var startInfo = new ProcessStartInfo("/usr/bin/osascript") {
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add(script);

            return Process.Start(startInfo);
        }

        private static string QuoteForShell(string value)
        {
            return "'" + value.Replace("'", "'\\''") + "'";
        }

        private static string QuoteForAppleScript(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// Try to register current process in a sudo session
        /// </summary>
        /// <param name="askPasswordPrompt"></param>
        /// <returns></returns>
        internal static async Task<bool> OsxTryAcquireElevation(string askPasswordPrompt)
        {
            // The following code solve issue where osascript is the only 
            // "dependencyless" way to run a graphical sudo command on osx
            // Unfortunately, osascript does not save the launching process root access
            // making fluxzy re-ask the password every time ca configuration changed, which is annoying
            // for the final user. 
            // sudo does save the session, so we had to do the following trick. 

            // First we check if we can already sudo 

            var canElevated = await ProcessUtilX.CanElevated().ConfigureAwait(false);

            if (canElevated)
            {
                // There is a very tight window between the check process and the actual start 
                // where the root timestamp may expired, in this case stdin may be blocked forever. 
                // Make sure that subsequent sudo process is running with the -n option 

                return true;
            }

            // Now we need to ask the password via osascript 

            var numberTries = 3;  // We tries 3 times

            for (int i = 0; i < numberTries; i++)
            {
                var result =
                    await AskForElevation(askPasswordPrompt).ConfigureAwait(false);

                if (result == PasswordElevationRequestResult.OK)
                    return true;

                if (result == PasswordElevationRequestResult.Refused)
                    break;

                // Otherwise, we try again
            }

            return false;
        }

        private static async Task<PasswordElevationRequestResult> AskForElevation(string askPasswordPrompt)
        {
            var osascript = new ProcessStartInfo("osascript", $"-e \"Tell application \\\"System Events\\\" " +
                                                              $"to display dialog \\\"{askPasswordPrompt}\\\" " +
                                                              $"default answer \\\"\\\" with hidden answer\" -e \"text returned of result\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            var osascriptProcess = Process.Start(osascript)!;

            var buffer = await osascriptProcess.StandardOutput.BaseStream.ToArrayGreedyAsync().ConfigureAwait(false);

            await osascriptProcess.WaitForExitAsync().ConfigureAwait(false);

            if (osascriptProcess.ExitCode != 0)
            {
                return PasswordElevationRequestResult.Refused;
            }

            try
            {
                var checkStartInfo = new ProcessStartInfo("sudo", "-S -v")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardInput = true
                };

                var checkStartProcess = Process.Start(checkStartInfo)!;

                await checkStartProcess.StandardInput.BaseStream.WriteAsync(buffer).ConfigureAwait(false);
                await checkStartProcess.StandardInput.BaseStream.DisposeAsync().ConfigureAwait(false);

                await checkStartProcess.WaitForExitAsync().ConfigureAwait(false);

                return checkStartProcess.ExitCode == 0 ? PasswordElevationRequestResult.OK : PasswordElevationRequestResult.BadPassword;
            }
            finally
            {
                Array.Clear(buffer,0, buffer.Length); // Remove the password to minimize attack window 
            }
        }

        internal enum PasswordElevationRequestResult
        {
            Refused,
            BadPassword,
            OK
        }
    }
}
