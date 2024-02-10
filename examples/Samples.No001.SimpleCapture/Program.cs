using System.Net;
using Fluxzy;

namespace Samples.No001.SimpleCapture
{
    internal class Program
    {
        /// <summary>
        /// In this sample, we will make a simple capture session and save it to an fxzy file or HAR file
        /// </summary>
        static async Task Main()
        {
            var tempDirectory = "capture_dump";

            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting
                                       // listen on port 44344 on IPV4 loopback
                                       .CreateDefault(IPAddress.Loopback, 44344)
                                       // add optional extra binding address on IPV6 loopback
                                       .AddBoundAddress(IPAddress.IPv6Loopback, 44344) 
                                        // set the temporary output directory
                                       .SetOutDirectory(tempDirectory)
                                       // instruct Fluxzy to install the certificate to the default machine store
                                       .SetAutoInstallCertificate(true);

            // Create a proxy instance
            await using (var proxy = new Proxy(fluxzyStartupSetting))
            {
                var endpoints = proxy.Run();

                using var httpClient = new HttpClient(new HttpClientHandler()
                {
                    // We instruct the HttpClient to use the proxy
                    Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                    UseProxy = true
                });

                // Make a request to a remote website
                using var response = await httpClient.GetAsync("https://www.fluxzy.io/hello");

                // Fluxzy is in full streaming mode, this means that the actual body content 
                // is only captured when the client reads it. 

                await (await response.Content.ReadAsStreamAsync()).CopyToAsync(Stream.Null);
            }

            // Packing the output files must be after the proxy dispose because some files may 
            // remain write-locked. 

            // Pack the files into fxzy file. This is the recommended file format as it can holds raw capture datas. 
            Packager.Export(tempDirectory, "mycapture.fxzy");

            // Pack the files into a HAR file
            Packager.ExportAsHttpArchive(tempDirectory, "mycapture.har");
        }


    }
}