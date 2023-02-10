using System.Diagnostics;
using System.Reflection;

namespace Fluxzy.Desktop.Ui.Runtime
{
    public static class AppControl
    {
        public static void PrepareForRun(string[] commandLineArgs, CancellationTokenSource cancellationTokenSource,
            out bool isDesktop)
        {
            isDesktop = false; 
            
            var runningInDesktop = commandLineArgs.Any(s => s.Equals("--desktop", StringComparison.OrdinalIgnoreCase));

            File.WriteAllText("c:\\pid.txt", string.Join(" | ", commandLineArgs));
            File.WriteAllText("c:\\currentdir.txt", Environment.CurrentDirectory);

            if (!runningInDesktop)
                return;

            Environment.SetEnvironmentVariable("Desktop", "true");


            SetCurrentDirectoryToAppDirectory();
            

            if (CommandLineUtility.TryGetArgsValue(commandLineArgs, "--fluxzyw-pid", out var fluxzywPidString))
            {

                if (int.TryParse(fluxzywPidString, out var fluxzywPid))
                {
                    ExitWhenParentExit(fluxzywPid, cancellationTokenSource);
                }
            }

            isDesktop = true; 
        }

        internal static void SetCurrentDirectoryToAppDirectory()
        {
            var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            Directory.SetCurrentDirectory(appDirectory);
        }

        internal static void ExitWhenParentExit(int parentPid, CancellationTokenSource cancellationTokenSource)
        {

            Task.Run(async () =>
            {
                try {
                    var parentProcess = Process.GetProcessById(parentPid);
                    await parentProcess.WaitForExitAsync();
                }
                catch {
                    // If parent process is not found, we just exit.
                }
                finally {
                    cancellationTokenSource.Cancel();
                }
            }); 
        }
    }
}
