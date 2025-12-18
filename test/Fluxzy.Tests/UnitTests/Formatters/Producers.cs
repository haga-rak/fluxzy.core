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
        [InlineData("https://sandbox.fluxzy.io/?a=1&b=2&c=3")]
        [InlineData(TestConstants.TestDomain)]
        [InlineData("https://sandbox.fluxzy.io/?a=&b=2&c=3")]
        [InlineData("https://sandbox.fluxzy.io/?a=1%201&b=2&c=3")]
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
                Assert.NotNull(result.Title);
                Assert.Null(result.ErrorMessage);
                Assert.NotNull(result.Type);
                Assert.Equal(queryNameValue.Count, result.Items.Count);

                foreach (var item in result.Items)
                {
                    Assert.Equal(queryNameValue[item.Name], item.Value);
                }
            }
        }

        [Theory]
        [InlineData(TestConstants.TestDomain, "leeloo", "multipass")]
        [InlineData(TestConstants.TestDomain, "leeloo", "")]
        [InlineData(TestConstants.TestDomain, "", "")]
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
        [InlineData(TestConstants.TestDomain, "leeloo")]
        [InlineData(TestConstants.TestDomain, "")]
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
        [InlineData(TestConstants.TestDomain, "leeloo")]
        [InlineData(TestConstants.TestDomain, "")]
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
        [InlineData(TestConstants.TestDomain, "")]
        [InlineData(TestConstants.TestDomain, "a=b;d=f")]
        [InlineData(TestConstants.TestDomain, "a=b;d=fà56789àç(_$£*")]
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
        [InlineData(TestConstants.TestDomain)]
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
            Assert.Equal($"GET /ip HTTP/1.1\r\nHost: {TestConstants.TestDomainHost}\r\nAccept-Encoding: gzip, deflate, br\r\n\r\n", result.RawHeader);
        }

        [Theory]
        [InlineData(TestConstants.TestDomain)]
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
        [InlineData(TestConstants.TestDomain, true)]
        [InlineData(TestConstants.TestDomain, false)]
        public async Task RequestJsonBodyProducer(string url, bool pass)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new RequestJsonBodyProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            
            if (pass)
                requestMessage.Content = new StringContent("{  }", Encoding.UTF8, "application/json");

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);
            
            if (pass) {
                Assert.NotNull(result);
                Assert.Equal("{  }", result.RawBody);
                Assert.Equal("{}", result.FormattedBody);
            }
            else {
                Assert.Null(result);
            }
        }

        [Theory]
        [InlineData(TestConstants.TestDomain)]
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
        [InlineData("https://sandbox.fluxzy.io/global-health-check", true)]
        [InlineData("https://sandbox.fluxzy.io/content-produce/0/0", false)]
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
                Assert.True(result.ContentLength > 0);
                Assert.Equal("application/json; charset=utf-8", result.ContentType);
            }
            else {
                Assert.Null(result);
            }
        }
        
        [Theory]
        [InlineData("https://sandbox.fluxzy.io/global-health-check", true)]
        [InlineData("https://sandbox.fluxzy.io/content-produce/0/0", false)]
        [InlineData(TestConstants.TestDomain, false)]
        public async Task ResponseBodyJsonProducer(string url, bool match)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new ResponseBodyJsonProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            
            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);
            
            if (match) {
                Assert.NotNull(result);
                Assert.NotNull(result.FormattedContent);
            }
            else {
                Assert.Null(result);
            }
        }
        
        [Theory]
        [InlineData("https://sandbox.fluxzy.io/global-health-check", true)]
        [InlineData("https://sandbox.fluxzy.io/content-produce/0/0", false)]
        public async Task ResponseTextContentProducer(string url, bool match)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new ResponseTextContentProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            
            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);
            
            if (match) {
                Assert.NotNull(result);
            }
            else {
                Assert.Null(result);
            }
        }
        [Theory]
        [InlineData("https://registry.2befficient.io:40300/cookies/set/abc/def", true)]
        [InlineData("https://sandbox.fluxzy.io/content-produce/0/0", false)]
        public async Task SetCookieProducer(string url, bool match)
        {
            var randomFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var producer = new SetCookieProducer();
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            
            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile);

            var (producerContext, firstExchange) = await Init(randomFile);

            var result = producer.Build(firstExchange, producerContext);
            
            if (match) {
                Assert.NotNull(result);
                Assert.Single(result.Cookies);
                Assert.Equal("abc", result.Cookies[0].Name);
                Assert.Equal("def", result.Cookies[0].Value);
                Assert.Null(result.Cookies[0].Domain);
                Assert.Equal("/", result.Cookies[0].Path);
                Assert.Null(result.Cookies[0].SameSite);
                Assert.False(result.Cookies[0].Secure);
                Assert.False(result.Cookies[0].HttpOnly);
            }
            else {
                Assert.Null(result);
            }
        }
        
        [Theory]
        [InlineData("https://www.fluxzy.io/assets/images/logo-small.png", true)]
        [InlineData("https://www.fluxzy.io/favicon.ico", true)]
        [InlineData(TestConstants.TestDomain, false)]
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
