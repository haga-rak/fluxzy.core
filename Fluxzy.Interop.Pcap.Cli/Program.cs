using System.Diagnostics;

namespace Fluxzy.Interop.Pcap.Cli
{
    internal class Program
    {
        private static async Task CancelTokenSourceOnStandardInputClose(CancellationTokenSource source)
        {
            string? str; 

            while ((str = await Console.In.ReadLineAsync()) != null && !str.Equals("exit", StringComparison.OrdinalIgnoreCase)) {

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
        /// 
        /// 
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

                // We halt the process when one of the task is completed
                await Task.WhenAny(loopingTask, stdInClose, parentMonitoringTask); 

                return 0;
            }
            catch (Exception) {
                // To do : connect logger here
                throw; 
            }
        }
    }

    public static class StreamDrainer
    {
        public static async Task<long> Drain(this Stream stream)
        {
            var buffer = new byte[32 * 1024];

            int read;
            long total = 0;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0) total += read;

            return total;
        }
    }
}