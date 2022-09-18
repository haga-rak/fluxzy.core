using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Text;

namespace Fluxzy.Interop.Pcap.Cli
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var loopBack = IPAddress.Loopback;
            var none = IPAddress.None;
            var any = IPAddress.Any;

            var mapToV4 = any.MapToIPv4();


            var _total = 0L;
            var totalLength = 20 * 1024 * 1024;
            var url = $"https://sandbox.smartizy.com/content-produce/{totalLength}/{totalLength}";
            var host = "sandbox.smartizy.com";

            var stopWatch = new Stopwatch();

            using (var captureContext = new CaptureContext())
            await using (var tcpClient = new CapturableTcpConnection(captureContext, "gogo2.pcap")) {
                var remoteIp = (await Dns.GetHostAddressesAsync(host)).First();

                await tcpClient.ConnectAsync(remoteIp, 443);

                stopWatch.Start();

                await using (var sslStream = new SslStream(tcpClient.GetStream(), false)) {
                    await sslStream.AuthenticateAsClientAsync(host);

                    var httpRequest =
                        $"GET {url} HTTP/1.1\r\n" +
                        $"Host: {host}\r\n" +
                        "Connection: close\r\n" +
                        "\r\n";

                    sslStream.Write(Encoding.UTF8.GetBytes(httpRequest));

                    //await Task.Delay(1000);

                    _total = await sslStream.Drain();

                    //  await Task.Delay(1000);
                }

                stopWatch.Stop();
            }


            Console.WriteLine($"Terminé en {stopWatch.ElapsedMilliseconds} ms");
            Console.ReadLine();
            GC.Collect();
            Console.WriteLine("GC done");
            Console.ReadLine();
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