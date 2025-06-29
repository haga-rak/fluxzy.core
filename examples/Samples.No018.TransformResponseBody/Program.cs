using Fluxzy;
using Fluxzy.Rules.Actions;
using System.Net;

namespace Samples.No018.TransformResponseBody
{
    internal class Program
    {
        /// <summary>
        ///  This example shows how to use response body transformation.
        ///  Response body transformation allows you to modify the response body according to the original request body content.
        /// </summary>
        /// <param name="args"></param>
        static async Task Main(string[] args)
        {
            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting.CreateLocalRandomPort();

            fluxzyStartupSetting.ConfigureRule()
                                .WhenAny() 
                                .Do(new TransformResponseBodyAction(async (transformContext, bodyReader) => {
                                    var content = await bodyReader.ConsumeAsString();

                                    // Use bodyReader.ConsumeAsStream() to avoid reading the body into memory
                                    // and process it as a stream

                                    return new BodyContent(content.ToUpperInvariant());
                                }));

            // Create a proxy instance
            await using var proxy = new Proxy(fluxzyStartupSetting);

            var endpoints = proxy.Run();

            using var httpClient = new HttpClient(new HttpClientHandler()
            {
                Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                UseProxy = true
            });

            using var response = await httpClient.GetAsync(
                "https://www.googleapis.com/books/v1/volumes?q=lords-of-the-rings");

            var contentString = await response.Content.ReadAsStringAsync();

            Console.ReadKey();
        }
    }
}
