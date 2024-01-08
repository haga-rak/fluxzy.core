using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Xunit;
using System.Threading.Tasks;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Formatters.Producers.Responses;
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

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

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

        [Theory]
        [InlineData("https://example.com", "")]
        [InlineData("https://example.com", "a=b;d=f")]
        [InlineData("https://example.com", "a=b;d=fà56789àç(_$£*")]
        public async Task FormUrlEncodedProducer(string url, string flatParams)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new FormUrlEncodedProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            var dictionary = flatParams.Split(";", StringSplitOptions.RemoveEmptyEntries)
                 .Select(s => s.Split("=", 2, StringSplitOptions.RemoveEmptyEntries))
                                        .ToDictionary(s => s[0], s => s[1]);

            requestMessage.Content = new FormUrlEncodedContent(dictionary); 

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile); 

            var result = producer.Build(firstExchange, producerContext);

            if (dictionary.Count == 0) {
                Assert.Null(result);
                return;
            }

            Assert.NotNull(result);
            Assert.Equal(dictionary.Count, result.Items.Count);

            foreach (var item in result.Items) {
                Assert.Equal(dictionary[item.Key], item.Value);
            }
        }

        [Theory]
        [InlineData("https://example.com")]
        public async Task RawRequestHeaderProducer(string url)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new RawRequestHeaderProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile); 

            var result = producer.Build(firstExchange, producerContext);
            
            Assert.NotNull(result);
            Assert.Equal("GET / HTTP/1.1\r\nHost: example.com\r\n\r\n", result.RawHeader);
        }

        [Theory]
        [InlineData("https://example.com")]
        public async Task RequestBodyAnalysis(string url)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new RequestBodyAnalysis();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);

            Assert.NotNull(result);
            Assert.Equal(2, result.BodyLength);
            Assert.Equal("application/json; charset=utf-8", result.ContentType);
        }

        [Theory]
        [InlineData("https://example.com")]
        public async Task RequestJsonBodyProducer(string url)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new RequestJsonBodyProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Content = new StringContent("{  }", Encoding.UTF8, "application/json");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);

            Assert.NotNull(result);
            Assert.Equal("{  }", result.RawBody);
            Assert.Equal("{}", result.FormattedBody);
        }

        [Theory]
        [InlineData("https://example.com")]
        public async Task RequestTextBodyProducer(string url)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new RequestTextBodyProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Content = new StringContent("{  }", Encoding.UTF8, "application/json");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);

            Assert.NotNull(result);
            Assert.Equal("{  }", result.Text);
        }
        
        [Theory]
        [InlineData("https://sandbox.smartizy.com/global-health-check", true)]
        [InlineData("https://sandbox.smartizy.com/content-produce/0/0", false)]
        public async Task ResponseBodySummaryProducer(string url, bool match)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new ResponseBodySummaryProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            
            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);
            
            if (match) {
                Assert.NotNull(result);
                Assert.NotNull(result.BodyText);
                Assert.NotNull(result.PreferredFileName);
                Assert.NotNull(result.Compression);
                Assert.Equal("application/json; charset=utf-8", result.ContentType);
            }
            else {
                Assert.Null(result);
            }
        }
        
        [Theory]
        [InlineData("https://www.fluxzy.io/assets/images/logo-small.png", true)]
        [InlineData("https://www.fluxzy.io/favicon.ico", true)]
        [InlineData("https://example.com", false)]
        public async Task ImageResultProducer(string url, bool image)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new ImageResultProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);

            if (image) {
                Assert.NotNull(result);
                Assert.Contains("image", result.ContentType);
            }
            else {
                Assert.Null(result);
            }
        }
    }
}
