using System;
using System.Net.Http;
using Xunit;
using System.Threading.Tasks;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Tests._Fixtures;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class Producers : FormatterTestBase
    {
        [Theory]
        [InlineData("https://example.com/?a=1&b=2&c=3")]
        [InlineData("https://example.com")]
        [InlineData("https://example.com/?a=&b=2&c=3")]
        [InlineData("https://example.com/?a=1%201&b=2&c=3")]
        public async Task QueryString(string url)
        {
            var producer = new QueryStringProducer();
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);
            var queryNameValue = System.Web.HttpUtility.ParseQueryString(uri.Query);

            await QuickArchiveBuilder.MakeQuickArchiveGet(url, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile); 

            var result = producer.Build(firstExchange, producerContext);

            if (queryNameValue.Count == 0) {
                Assert.Null(result);
            }
            else {
                Assert.NotNull(result);
                Assert.Equal(queryNameValue.Count, result.Items.Count);

                foreach (var item in result.Items)
                {
                    Assert.Equal(queryNameValue[item.Name], item.Value);
                }
            }
        }

        [Theory]
        [InlineData("https://example.com", "leeloo", "multipass")]
        [InlineData("https://example.com", "leeloo", "")]
        [InlineData("https://example.com", "", "")]
        public async Task AuthorizationBasic(string url, string userName, string password)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new AuthorizationBasicProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            var authenticationString = $"{userName}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));

            requestMessage.Headers.Add("Authorization", $"Basic {base64EncodedAuthenticationString}");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile); 

            var result = producer.Build(firstExchange, producerContext);

            Assert.NotNull(result);
            Assert.Equal(userName, result.ClientId);
            Assert.Equal(password, result.ClientSecret);
        }

        [Theory]
        [InlineData("https://example.com", "leeloo")]
        [InlineData("https://example.com", "")]
        public async Task AuthorizationBearer(string url, string token)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new AuthorizationBearerProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            requestMessage.Headers.Add("Authorization", $"Bearer {token}");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile); 

            var result = producer.Build(firstExchange, producerContext);

            Assert.NotNull(result);
            Assert.Equal(token, result.Token);
        }

        [Theory]
        [InlineData("https://example.com", "leeloo")]
        [InlineData("https://example.com", "")]
        public async Task AuthorizationProducer(string url, string token)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new AuthorizationProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            requestMessage.Headers.TryAddWithoutValidation("Authorization", $"{token}");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile); 

            var result = producer.Build(firstExchange, producerContext);

            Assert.NotNull(result);
            Assert.Equal(token, result.Value);
        }
    }
}
