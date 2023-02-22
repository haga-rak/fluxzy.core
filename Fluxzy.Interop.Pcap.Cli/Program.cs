using System.Diagnostics;

namespace Fluxzy.Interop.Pcap.Cli
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length < 2 ) {
                Console.WriteLine("Usage : command pipeName pid");
                return 1; 
            }
            var pipeName = args[0];
            var processId = int.Parse(args[1]);
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token; 
            var parentProcess = Process.GetProcessById(processId);

            

            await parentProcess.WaitForExitAsync(cancellationTokenSource.Token); 
            Console.ReadLine();

            return 0;
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