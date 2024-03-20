using System.Diagnostics;
using Fluxzy.Core.Pcap.Pcapng;

namespace Samples.No014.HttpClientWithPcap
{
    internal class Program
    {
        /// <summary>
        ///   Capturing HTTP traffic with HttpClient and saving it to a pcapng file.
        /// </summary>
        /// <returns></returns>
        static async Task Main()
        {
            // raw captured file
            var tempFile = $"output.raw.pcapng";

            // captured file combined with keys
            var decodedFile = $"output.decoded.pcapng";

            {
                // Handler must be disposed to access the captured data

                using var handler = await PcapngUtils.CreateHttpHandler(tempFile);
                using var httpClient = new HttpClient(handler);
                using var _ = await httpClient.GetAsync("https://www.example.com");
            }

            await using var outStream = File.Create(decodedFile);

            // Utility to read the pcapng file with the included keys if available
            await PcapngUtils.ReadWithKeysAsync(tempFile).CopyToAsync(outStream);

            // Fail if you don't have Wireshark installed or a compatible pcapng reader
            Process.Start(new ProcessStartInfo(decodedFile)
            {
                UseShellExecute = true
            });
        }
    }
}
