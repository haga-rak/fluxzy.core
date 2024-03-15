using System.Net;
using Fluxzy;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Extensions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Samples.No002.Filtering
{
	internal class Program
	{

        static async Task Do()
        {
            var fluxzyStartupSetting = FluxzySetting
                                       .CreateDefault(IPAddress.Loopback, 44344)
                                       .AddAlterationRules(
                                           new Rule(
                                               new AddResponseHeaderAction("X-Proxy", "Passed through fluxzy"),
                                               AnyFilter.Default
                                           ));

            await using (var proxy = new Proxy(fluxzyStartupSetting))
            {
                var _ = proxy.Run();

                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }
        }

		/// <summary>
		/// Capture only specific traffic.  
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static async Task Main(string[] args)
		{
			var tempDirectory = "filtered_dump";

			// Create a default run settings 
            var fluxzyStartupSetting = FluxzySetting
			                           .CreateDefault(IPAddress.Loopback, 44344)
			                           .SetOutDirectory(tempDirectory);

            // Only filter with OnAuthorityReceived scope will be accepted
            fluxzyStartupSetting.SetSaveFilter(new HostFilter("fluxzy.io", StringSelectorOperation.EndsWith));

            // We can combine multiple condition wit a filter collection 
            fluxzyStartupSetting.SetSaveFilter(new FilterCollection(
                new HostFilter("fluxzy.io", StringSelectorOperation.EndsWith),
                new IsSecureFilter()
            ) {
                Operation = SelectorCollectionOperation.And
            });

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
            }

			// Packing the output files must be after the proxy dispose because some files may 
			// remain write-locked. 

			// Pack the files into fxzy file. This is the recommended file format as it can holds raw capture datas. 
			Packager.Export(tempDirectory, "filtered_dump.fxzy");
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