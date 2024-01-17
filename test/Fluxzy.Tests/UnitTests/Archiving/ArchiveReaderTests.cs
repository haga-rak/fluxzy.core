// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.Clients.H11;
using Fluxzy.Formatters.Metrics;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Utils;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    public abstract class ArchiveReaderTests
    {
        private readonly IArchiveReader _archiveReader;

        protected ArchiveReaderTests(IArchiveReader archiveReader)
        {
            _archiveReader = archiveReader;
        }

        [Fact]
        public void ReadMetaInformation()
        {
            var metaInformation = _archiveReader.ReadMetaInformation();
            Assert.NotNull(metaInformation);
            Assert.NotNull(metaInformation.ArchiveVersion);
            Assert.NotNull(metaInformation.FluxzyVersion);
            Assert.NotNull(metaInformation.Tags);
            Assert.NotNull(metaInformation.ViewFilters);
            Assert.NotEqual(default, metaInformation.CaptureDate);
        }

        [Fact]
        public void Invalid_Exchange()
        {
            var result = _archiveReader.ReadExchange(9556461);
            Assert.Null(result);
        }

        [Fact]
        public void Read_DownStreamErrors()
        {
            var exchanges = _archiveReader.ReaderAllDownstreamErrors()?.ToList();

            Assert.NotNull(exchanges!);
        }

        [Fact]
        public void Read_Exchanges()
        {
            var exchanges = _archiveReader.ReadAllExchanges()?.ToList();

            Assert.NotNull(exchanges!);
            Assert.NotEmpty(exchanges);

            foreach (var exchange in exchanges) {
                var result = _archiveReader.ReadExchange(exchange.Id);

                _ = _archiveReader.HasResponseBody(exchange.Id);
                var hasRequestBody = _archiveReader.HasRequestBody(exchange.Id);
                
                var decodedRequestBody =
                    _archiveReader.GetDecodedRequestBody(exchange.Id)?.Drain() ?? -1;

                var isWebSocket = exchange.IsWebSocket; 


                Assert.NotNull(result);
                Assert.True(result.Done);
                Assert.True(result.Sent > 0);
                Assert.True(result.Received > 0);
                Assert.NotNull(ExchangeUtility.GetRequestBodyFileNameSuggestion(exchange));
                Assert.NotNull(ExchangeUtility.GetResponseBodyFileNameSuggestion(exchange));

                if (hasRequestBody) {
                    Assert.True(decodedRequestBody > 0);
                }
                else {
                    Assert.Equal(-1, decodedRequestBody);
                }

                if (isWebSocket) {
                    Assert.NotNull(exchange.WebSocketMessages!);
                    foreach (var message in exchange.WebSocketMessages) {
                        var sendLength = _archiveReader.GetRequestWebsocketContent(exchange.Id,
                            message.Id)?.Drain() ?? -1;

                        var receiveLength = _archiveReader.GetResponseWebsocketContent(exchange.Id,
                            message.Id)?.Drain() ?? -1;

                        Assert.NotNull(message.Data);

                        Assert.Equal(-1, receiveLength);
                        Assert.Equal(-1, sendLength);
                    }
                }
            }
        }

        [Fact]
        public void Validate_Connections()
        {
            var connections = _archiveReader.ReadAllConnections()?.ToList();

            Assert.NotNull(connections!);
            Assert.NotEmpty(connections);

            foreach (var connection in connections) {
                var result = _archiveReader.ReadConnection(connection.Id);
                Assert.NotNull(result);

                Assert.True(_archiveReader.HasCapture(connection.Id));

                using var rawCaptureStream = _archiveReader.GetRawCaptureStream(connection.Id);
                using var rawCaptureKeyStream = _archiveReader.GetRawCaptureKeyStream(connection.Id);

                Assert.NotNull(rawCaptureStream!);
                Assert.NotNull(rawCaptureKeyStream!);
                var length = rawCaptureStream.Drain();

                var keyStreamLength = rawCaptureKeyStream.Drain();

                Assert.True(length > 0);
                Assert.True(keyStreamLength > 0);

                if (_archiveReader is DirectoryArchiveReader directoryArchiveReader) {
                    var rawCaptureFile = directoryArchiveReader.GetRawCaptureFile(connection.Id);
                    Assert.NotNull(rawCaptureFile!);
                }
            }
        }

        [Fact]
        public void Validate_Metric_Builder()
        {
            var exchange = _archiveReader.ReadAllExchanges()?.First()!;
            var metricBuilder = new ExchangeMetricBuilder();

            var metricInfo = metricBuilder.Get(exchange.Id, _archiveReader);

            Assert.NotNull(metricInfo);
            Assert.True(metricInfo.Available);
            Assert.NotNull(metricInfo.RequestBody);
            Assert.NotNull(metricInfo.Dns);
            Assert.NotNull(metricInfo.OverAllDuration);
            Assert.NotNull(metricInfo.Waiting);
            Assert.NotNull(metricInfo.RequestHeader);
            Assert.NotNull(metricInfo.ReceivingHeader);
            Assert.NotNull(metricInfo.ReceivingBody);
            Assert.NotNull(metricInfo.Queued);
            Assert.NotNull(metricInfo.RawMetrics);
            Assert.NotNull(metricInfo.TcpHandShake);
            Assert.NotNull(metricInfo.SslHandShake);
        }
    }
}
