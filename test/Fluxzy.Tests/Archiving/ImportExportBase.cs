// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Archiving.Har;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Archiving
{
    public class FxzyToHarToFxzy : ImportExportBase
    {
        [Fact]
        public async Task Post_With_Short_Text_Payload()
        {
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    $"{TestConstants.GetHost("http2")}/global-health-check");

            requestMessage.Content = new ByteArrayContent("ABCD"u8.ToArray());

            await Capture_Export_And_Import(requestMessage);
        }

        [Fact]
        public async Task Post_With_Large_Payload()
        {
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    $"{TestConstants.GetHost("http2")}/global-health-check");

            var payload = new byte[1024 * 200 + 253];
            new Random(9).NextBytes(payload);

            var length = Convert.ToBase64String(payload).Length;

            requestMessage.Content = new ByteArrayContent(payload);

            await Capture_Export_And_Import(requestMessage);
        }

        [Fact]
        public async Task Simple_Get()
        {
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get,
                    $"{TestConstants.GetHost("http2")}/global-health-check");

            await Capture_Export_And_Import(requestMessage);
        }
    }

    public class ImportExportBase
    {
        protected async Task Capture_Export_And_Import(HttpRequestMessage requestMessage)
        {
            var sessionIdentifier = Guid.NewGuid().ToString();

            var outFileName = "test-artifacts/" + sessionIdentifier + ".har";
            var outHarDirectory = "test-artifacts/" + sessionIdentifier;

            var (exchangeInfo, connectionInfo, originalDirectoryArchiveReader) =
                await RequestHelper.DirectRequest(requestMessage);

            var packager = new HttpArchivePackager(new HttpArchiveSavingSetting {
                Policy = HttpArchiveSavingBodyPolicy.AlwaysSave
            });

            await using (var fileStream = File.Create(outFileName)) {
                await packager.Pack(originalDirectoryArchiveReader.BaseDirectory, fileStream, null);
            }

            new HarImportEngine().WriteToDirectory(outFileName, outHarDirectory);

            var fullHar = File.ReadAllText(outFileName);

            var directoryArchiveReader = new DirectoryArchiveReader(outHarDirectory);

            var actualExchangeInfos = directoryArchiveReader.ReadAllExchanges().ToList();
            var actualConnectionInfos = directoryArchiveReader.ReadAllConnections().ToList();

            Assert.Equal(1, actualExchangeInfos.Count);
            Assert.Equal(1, actualConnectionInfos.Count);

            var actualExchangeInfo = actualExchangeInfos.First();
            var actualConnectionInfo = actualConnectionInfos.First();

            var expectedRequestBody = originalDirectoryArchiveReader.GetRequestBody(exchangeInfo.Id)
                                                                    ?.ToBase64String(true) ?? string.Empty;

            var requestBody = directoryArchiveReader.GetRequestBody(actualExchangeInfo.Id)
                                                    ?.ToBase64String(true) ?? string.Empty;

            var expectedResponseBody = originalDirectoryArchiveReader.GetResponseBody(exchangeInfo.Id)
                                                                     ?.ToBase64String(true) ?? string.Empty;

            var responseBody = directoryArchiveReader.GetResponseBody(actualExchangeInfo.Id)
                                                     ?.ToBase64String(true) ?? string.Empty;

            Assert.Equal(exchangeInfo.Agent, actualExchangeInfo.Agent);
            Assert.Equal(exchangeInfo.Comment, actualExchangeInfo.Comment);
            Assert.Equal(exchangeInfo.ConnectionId, actualExchangeInfo.ConnectionId);
            Assert.Equal(exchangeInfo.ContentType, actualExchangeInfo.ContentType);
            Assert.Equal(exchangeInfo.Done, actualExchangeInfo.Done);
            Assert.Equal(exchangeInfo.EgressIp, actualExchangeInfo.EgressIp);

            Assert.Equal(exchangeInfo.FullUrl, actualExchangeInfo.FullUrl);
            Assert.Equal(exchangeInfo.KnownAuthority, actualExchangeInfo.KnownAuthority);
            Assert.Equal(exchangeInfo.KnownPort, actualExchangeInfo.KnownPort);
            Assert.Equal(exchangeInfo.Secure, actualExchangeInfo.Secure);
            Assert.Equal(exchangeInfo.Method, actualExchangeInfo.Method);
            Assert.Equal(exchangeInfo.Path, actualExchangeInfo.Path);
            Assert.Equal(exchangeInfo.StatusCode, actualExchangeInfo.StatusCode);
            Assert.Equal(exchangeInfo.IsWebSocket, actualExchangeInfo.IsWebSocket);
            Assert.Equal(exchangeInfo.RequestHeader, actualExchangeInfo.RequestHeader);
            Assert.Equal(exchangeInfo.ResponseHeader!.StatusCode, actualExchangeInfo.ResponseHeader!.StatusCode);

            Assert.Equal(exchangeInfo.ResponseHeader.Headers
                                     .Where(t => !t.Name.Span.Equals("Transfer-Encoding",
                                         StringComparison.OrdinalIgnoreCase)),
                actualExchangeInfo.ResponseHeader.Headers);

            Assert.Equal(expectedRequestBody.Length, requestBody.Length);
            Assert.Equal(expectedRequestBody, requestBody);
            Assert.Equal(expectedResponseBody, responseBody);
        }
    }
}
