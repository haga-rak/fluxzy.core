using System.Diagnostics;
using System.Reflection;
using System.Text;
using Fluxzy.Desktop.Ui.ViewModels;

namespace Fluxzy.Desktop.Ui.Runtime
{
    public static class AppControl
    {
        public static void PrepareForRun(string[] commandLineArgs, CancellationTokenSource cancellationTokenSource,
            out bool isDesktop)
        {
            isDesktop = false; 
            
            var runningInDesktop = commandLineArgs.Any(s => s.Equals("--desktop", StringComparison.OrdinalIgnoreCase));
            
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


        public static async Task AnnounceFileOpeningRequest(string fileName)
        {
            using var httpClient = new HttpClient(new HttpClientHandler()
            {
                UseProxy = false,

            });

            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var payload = new FileOpeningRequestViewModel(fileName);

            var payloadString = System.Text.Json.JsonSerializer.Serialize(payload,
                GlobalArchiveOption.DefaultSerializerOptions);

            using var res = await httpClient.PostAsync("http://localhost:5198/api/file/opening-request",
                new StringContent(payloadString, Encoding.UTF8, "application/json"));



        }
        
    }
}
