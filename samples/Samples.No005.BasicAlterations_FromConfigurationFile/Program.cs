using System.Net;
using Fluxzy;

namespace Samples.No005.BasicAlterations_FromConfigurationFile
{
    internal class Program
    {
        /// <summary>
        /// This sample shows how to alter the request and response via actions and filters.
        /// Actions and filters are defined from a yaml-configuration file. https://www.fluxzy.io/rule/syntax
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var tempDirectory = "basic_alteration_with_configuration_file";

            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting
                                       .CreateDefault(IPAddress.Loopback, 44344)
                                       .SetOutDirectory(tempDirectory);

            // The full list of available actions and rules are available at 
            // https://www.fluxzy.io/rule/search

            var yamlContent = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: AddRequestHeaderAction
                      headerName: fluxzy
                      headerValue: on
                  - filter: 
                      typeKind: StatusCodeRedirectionFilter        
                    action : 
                      typeKind: ApplyCommentAction
                      comment: This is a redirection
                """;

            fluxzyStartupSetting.AddAlterationRules(yamlContent);

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