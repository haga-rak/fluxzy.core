using System.Diagnostics;

namespace Fluxzy.Interop.Pcap.Cli
{
    internal class Program
    {
        private static async Task CancelTokenSourceOnStandardInputClose(CancellationTokenSource source)
        {
            while (await Console.In.ReadLineAsync() is { } str
                   && !str.Equals("exit", StringComparison.OrdinalIgnoreCase)) {

            }

            // STDIN has closed this means that parent request halted or request an explicit close 

            if (source.IsCancellationRequested) {
                source.Cancel();
            }
        }

        private static async Task CancelTokenWhenParentProcessExit(CancellationTokenSource source, int processId)
        {
            Process process = Process.GetProcessById(processId);

            await process.WaitForExitAsync(source.Token);

            if (source.IsCancellationRequested)
            {
                source.Cancel();
            }
        }

        /// <summary>
        /// args[0] => caller PID 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static async Task<int> Main(string[] args)
        {
            try {

                if (args.Length < 1 ) {
                    Console.WriteLine("Usage : command pid. Received args : " + string.Join(" ", args));
                    return 1; 
                }
            
                if (!int.TryParse(args[0], out var processId)) {
                    Console.WriteLine("Process ID is not a valid integer");
                    return 2; 
                }
            
                var haltSource = new CancellationTokenSource();

                var stdInClose = Task.Run(async () => { await CancelTokenSourceOnStandardInputClose(haltSource); });
                var parentMonitoringTask = Task.Run(async () => { await CancelTokenWhenParentProcessExit(haltSource, processId); });  

                await using var receiverContext = new PipeMessageReceiverContext(new DirectCaptureContext(), haltSource.Token);
                
                receiverContext.Start();
                Console.WriteLine(receiverContext.Receiver!.ListeningPort);
            
                var loopingTask = receiverContext.WaitForExit();

                // We halt the process when one of the following task is complete task is completed
                await Task.WhenAny(loopingTask, stdInClose, parentMonitoringTask); 

                return 0;
            }
            catch (Exception ex) {
                // To do : connect logger here
                File.WriteAllText("d:\\logo.txt", ex.ToString());
                throw; 
            }
        }
    }
}