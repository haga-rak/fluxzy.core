// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests.Common;

namespace Fluxzy.Tests.Rules
{
    /// <summary>
    /// A generic class for testing filters
    /// </summary>
    public abstract class FilterTestTemplate
    {
        protected async Task<bool> CheckPass(HttpRequestMessage requestMessage, Filter filter)
        {
            await using var proxy = new AddHocConfigurableProxy(1, 10);

            var witnessComment = Guid.NewGuid().ToString();

            proxy.StartupSetting.AlterationRules.Add(
                new Rule(
                    new ApplyCommentAction(witnessComment),
                    filter));

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            using var response = await httpClient.SendAsync(requestMessage);

            await (await response.Content.ReadAsStreamAsync()).DrainAsync();

            return proxy.CapturedExchanges.Any(c => c.Comment == witnessComment);
        }
    }
}
