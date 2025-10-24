using Fluxzy;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.RequestFilters;
using System.Net;
using System.Text;

namespace Samples.No017.TransformRequestBody
{
    internal class Program
    {
        /// <summary>
        ///  This example shows how to use request body transformation.
        ///  Request body transformation allows you to modify the request body according to the original request body content.
        /// </summary>
        /// <param name="args"></param>
        static async Task Main(string[] args)
        {
            // Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting.CreateLocalRandomPort();

            // When a json request is received, the body will be transformed to uppercase.
            fluxzyStartupSetting.ConfigureRule()
                                .WhenAny(new JsonRequestFilter()) // take json only
                                .Do(new TransformRequestBodyAction(async (transformContext, bodyReader) => {
                                    var content = await bodyReader.ConsumeAsString();

                                    // Use bodyReader.ConsumeAsStream() to avoid reading the body into memory

                                    return content.ToUpperInvariant();
                                }));

            // Create a proxy instance
            await using var proxy = new Proxy(fluxzyStartupSetting);

            var endpoints = proxy.Run();

            using var httpClient = new HttpClient(new HttpClientHandler()
            {
                Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                UseProxy = true
            });

            // Make a request to the httpbin service /anything

            using var response = await httpClient.PostAsync("https://httpbin.org/anything"
                , new StringContent("{\"hello\":\"world\"}", Encoding.UTF8, "application/json"));

            var contentString = await response.Content.ReadAsStringAsync();
            var httpBinResponse = System.Text.Json.JsonSerializer.Deserialize<HttpBinResponse>(contentString)!;

            var result = httpBinResponse.Data;

            Console.WriteLine(result);

            Console.ReadKey();
        }
    }


    internal class HttpBinResponse
    {
        public HttpBinResponse(string data)
        {
            Data = data;
        }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string Data { get;  }
    }
}
