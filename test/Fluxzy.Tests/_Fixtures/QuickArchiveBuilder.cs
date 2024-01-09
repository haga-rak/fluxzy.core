// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Tests._Fixtures
{
    internal static class QuickArchiveBuilder
    {
        public static async Task MakeQuickArchive(HttpRequestMessage requestMessage,
             string fileName, Action<FluxzySetting> ? customizeSetting = null)
        {
            var id = Guid.NewGuid();
            var outDirectory = $"{nameof(QuickArchiveBuilder)}/{id}"; 

            var setting = 
                FluxzySetting.CreateDefault(IPAddress.Loopback, 0)
                             .SetOutDirectory(outDirectory);

            customizeSetting?.Invoke(setting);

            try {

                await using (var proxy = new Proxy(setting))
                {
                    var endPoint = proxy.Run().First();

                    using var client = new HttpClient(new HttpClientHandler()
                    {
                        Proxy = new WebProxy($"127.0.0.1:{endPoint.Port}"),
                        AllowAutoRedirect = false,
                        AutomaticDecompression = DecompressionMethods.All
                    });

                    using var response = await client.SendAsync(requestMessage);

                    await using var responseStream = await response.Content.ReadAsStreamAsync();

                    await responseStream.DrainAsync();
                }

                Packager.Export(outDirectory, fileName);
            }
            finally {

                if (Directory.Exists(outDirectory))
                    Directory.Delete(outDirectory, true);
            }
        }

        public static Task MakeQuickArchiveGet(string url, 
            string fileName, Action<FluxzySetting> ? customizeSetting = null)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            return MakeQuickArchive(httpRequestMessage, fileName, customizeSetting);
        }
    }
}
