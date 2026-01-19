using System.Net;
using Fluxzy;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Samples.No022.RejectRequests
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Loopback, 44344);

            fluxzySetting.ConfigureRule()
                // Simple 403 Forbidden
                .WhenHostMatch("blocked.example.com").Reject()

                // Block with custom status code
                .WhenHostMatch("hidden.example.com").Reject(404)

                // Block with custom message
                .WhenHostMatch("policy.example.com")
                .Reject(403, "Access denied by corporate policy")

                // Block with JSON response for API endpoints
                .When(new HostFilter("api.blocked.com"))
                .Do(new RejectWithMessageAction(403,
                    "{\"error\": \"forbidden\", \"message\": \"This API is blocked\"}",
                    "application/json"));

            await using var proxy = new Proxy(fluxzySetting);
            var endpoints = proxy.Run();

            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                UseProxy = true
            };

            using var httpClient = new HttpClient(handler);

            // Test blocked requests
            await TestRequest(httpClient, "https://blocked.example.com/resource");
            await TestRequest(httpClient, "https://hidden.example.com/secret");
            await TestRequest(httpClient, "https://policy.example.com/page");
            await TestRequest(httpClient, "https://api.blocked.com/v1/data");
        }

        static async Task TestRequest(HttpClient client, string url)
        {
            try
            {
                using var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{url} => {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"  Body: {body}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{url} => Error: {ex.Message}");
            }
        }
    }
}
