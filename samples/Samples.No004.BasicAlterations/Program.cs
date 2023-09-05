using System.Net;
using Fluxzy;
using Fluxzy.Certificates;
using Fluxzy.Clients.Mock;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Samples.No004.BasicAlterations
{
    internal class Program
    {
        /// <summary>
        /// This sample shows how to alter the request and response via actions and filters. 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var tempDirectory = "basic_alteration";

            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting
                                       .CreateDefault(IPAddress.Loopback, 44344)
                                       .SetOutDirectory(tempDirectory);

            // The full list of available actions and rules are available at 
            // https://www.fluxzy.io/rule/search
            
            fluxzyStartupSetting.AddAlterationRules(
                // Append "fluxzy-on" header to any request 
                new Rule(
                    new AddRequestHeaderAction("fluxzy-on", "true"),
                    new AnyFilter()
                ), 

                // Remove any cache directive from any request 
                new Rule(
                    new RemoveCacheAction(),
                    new AnyFilter()
                ), 

                // Avoid decrypting particular host 
                new Rule(
                    new SkipSslTunnelingAction(),
                    new HostFilter("secure.domain.com", StringSelectorOperation.Exact)
                ),

                // Mock an entire response according to an URL 
                new Rule(
                    new MockedResponseAction(
                        new MockedResponseContent(
                            200, 
                            Body.CreateFromString("This is a plain text content", "text/plain")
                                       .AddHeader("server", "fluxzy"))
                        ),
                    new AbsoluteUriFilter(@"^https\:\/\/api\.example\.com", StringSelectorOperation.Regex)
                ),

                // Using a client certificate
                new Rule(
                    new SetClientCertificateAction(Certificate.LoadFromUserStoreBySerialNumber("xxxxxx")),
                    new HostFilter("domain.with.mandatory.client.cert.com", StringSelectorOperation.Exact)
                )
                );
            
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

                await QueryAndForget(httpClient, HttpMethod.Get, "https://www.fluxzy.io/");
                await QueryAndForget(httpClient, HttpMethod.Get, "https://www.google.com/");
                await QueryAndForget(httpClient, HttpMethod.Get, "https://api.example.com/random_endpoint");
            }

            // Packing the output files must be after the proxy dispose because some files may 
            // remain write-locked. 

            // Pack the files into fxzy file. This is the recommended file format as it can holds raw capture datas. 
            Packager.Export(tempDirectory, "altered.fxzy");
        }

        /// <summary>
        /// Make a simple query with HttpClient
        /// </summary>
        /// <param name="client"></param>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        static async Task QueryAndForget(HttpClient client, HttpMethod method, string url)
        {
            // Make a request to a remote website
            using var response = await client.SendAsync(new HttpRequestMessage(method, url));

            var contentStream = await response.Content.ReadAsStreamAsync();

            // Fluxzy is in full streaming mode, this means that the actual body content 
            // is only captured when the client reads it.
            // Here we drain the response stream to an Stream.Null
            await contentStream.CopyToAsync(Stream.Null);
        }
    }
}