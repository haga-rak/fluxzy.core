// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.Tests.Archiving.Fixtures;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class HttpArchive : IClassFixture<HarFileFixture>
    {
        private readonly HarFileFixture _fixture;

        public HttpArchive(HarFileFixture fixture)
        {
            _fixture = fixture;
        }

        // [Fact]
        public void CheckMainHarEntryProperties()
        {
            var document = _fixture.Document.RootElement;
            var log = document.GetProperty("log");
            var entries = log.GetProperty("entries").EnumerateArray().ToList();
            var exchanges = _fixture.Exchanges;

            var str = document.ToString();

            Assert.Equal("1.2", log.GetProperty("version").GetString());
            Assert.Equal(exchanges.Count, entries.Count);

            foreach (var exchange in exchanges) {
                var entry = entries.FirstOrDefault(e => e.GetProperty("_exchangeId")
                                                         .GetInt32() == exchange.Id);

                Assert.NotEqual(default, entry);

                Assert.Equal(exchange.RequestHeader.Method.ToString(),
                    entry.GetProperty("request")
                         .GetProperty("method")
                         .GetString());

                Assert.Equal(exchange.RequestHeader.GetFullUrl(),
                    entry.GetProperty("request")
                         .GetProperty("url")
                         .GetString());

                foreach (var requestHeader in exchange.RequestHeader.Headers) {
                    var header = entry.GetProperty("request")
                                      .GetProperty("headers")
                                      .EnumerateArray()
                                      .FirstOrDefault(h => h.GetProperty("name")
                                                            .GetString() == requestHeader.Name.ToString()
                                                           && h.GetProperty("value")
                                                               .GetString() == requestHeader.Value.ToString());

                    Assert.NotEqual(default, header);
                }

                if (exchange.ResponseHeader != null) {
                    foreach (var responseHeader in exchange.ResponseHeader.Headers) {
                        var header = entry.GetProperty("response")
                                          .GetProperty("headers")
                                          .EnumerateArray()
                                          .FirstOrDefault(h => h.GetProperty("name")
                                                                .GetString() == responseHeader.Name.ToString()
                                                               && h.GetProperty("value")
                                                                   .GetString() == responseHeader.Value.ToString());

                        Assert.NotEqual(default, header);
                    }
                }
            }
        }
    }
}
