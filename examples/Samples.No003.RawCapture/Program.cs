using System.Net;
using Fluxzy;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Pcapng;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Samples.No003.RawCapture
{
    internal class Program
    {
        /// <summary>
        /// This short sample show how to enable raw capture with Fluxzy with out without capturing the NSS key log file. 
        /// The following code need to be run with administrator/root privilege.
        ///
        /// Fluxzy.Core.Pcap library is required for this sample to work.
        /// </summary>
        /// <returns></returns>
        static async Task Main()
        {
            var tempDirectory = "raw_capture_dump";
            var extractNssKey = true;  // Change this value in order to enable/disable NSS key log file capture.

            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting
                                       // listen on port 44344 on IPV4 loopback
                                       .CreateDefault(IPAddress.Loopback, 44344)
                                       // add optional extra binding address on IPV6 loopback
                                       .AddBoundAddress(IPAddress.IPv6Loopback, 44344)
                                       // set the temporary output directory
                                       .SetOutDirectory(tempDirectory);

            if (extractNssKey) {
                // To enable nss key capture, the SSL engine used by Fluxzy must be BouncyCastle 
                fluxzyStartupSetting.UseBouncyCastleSslEngine(); 
            }
            
            await using (var tcpConnectionProvider = await CapturedTcpConnectionProvider.CreateInProcessCapture()) {
                await using var proxy = new Proxy(fluxzyStartupSetting, tcpConnectionProvider: tcpConnectionProvider);

                var endpoints = proxy.Run();

                using var httpClient = new HttpClient(new HttpClientHandler() {
                    // We instruct the HttpClient to use the proxy
                    Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                    UseProxy = true
                });

                // Make a request to a remote website
                using var response = await httpClient.GetAsync("https://www.example.com/");

                // Fluxzy is in full streaming mode, this means that the actual body content 
                // is only captured when the client reads it. 
                await (await response.Content.ReadAsStreamAsync()).CopyToAsync(Stream.Null);
            }

            // Pack the files into fxzy file. This is the recommended file format as it can holds raw capture datas. 
            Packager.Export(tempDirectory, "mycapture.fxzy");

            // Exporting pcapng file 

            var archiveReader = new DirectoryArchiveReader(tempDirectory);

            var exchange = archiveReader.ReadAllExchanges().First(e => e.FullUrl == "https://www.example.com/");

            var rawCaptureStream = archiveReader.GetRawCaptureStream(exchange.ConnectionId);

            if (extractNssKey) {
                // Extract SSL key log file 
                var sslKeyLogContent = archiveReader.GetRawCaptureKeyStream(exchange.ConnectionId)!.ReadToEndGreedy();

                // Fluxzy provides an utility to combine a pcapng file with a SSLKeyLogFile 

                await using var pcanPngFile = File.Create("out.with-keys.pcapng");
                await PcapngUtils.CreatePcapngFileWithKeysAsync(sslKeyLogContent, rawCaptureStream!, pcanPngFile);
            }
            else
            {
                await using var fileStream = File.Create("out.pcapng");
                await rawCaptureStream!.CopyToAsync(fileStream);
            }
        }
        
    }
}