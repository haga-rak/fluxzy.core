// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Fluxzy.Archiving.Extensions;

namespace Fluxzy.Archiving.Saz
{
    /// <summary>
    /// An experimental saz packager reverse engineered from a random .saz, use at your own risk.
    /// </summary>
    [PackagerInformation("saz", "saz archive format", ".saz")]
    public class SazPackager : DirectoryPackager
    {
        public override bool ShouldApplyTo(string fileName)
        {
            return fileName.EndsWith(".saz", StringComparison.CurrentCultureIgnoreCase);
        }

        /// <inheritdoc />
        public override async Task Pack(string directory, Stream outputStream, HashSet<int>? exchangeIds)
        {
            var baseDirectory = new DirectoryInfo(directory);

            if (!baseDirectory.Exists)
                throw new InvalidOperationException("Directory does not exists");

            var packableFiles = GetPackableFileInfos(baseDirectory, exchangeIds).ToList();

            var connectionInfos = ReadConnections(packableFiles).ToDictionary(t => t.Id, t => t);

            var sessionId = 0;

            var exchanges = ReadExchanges(packableFiles).ToList();

            using var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create);
            var max = (int) Math.Ceiling(Math.Log10(exchanges.Count));

            foreach (var exchangeInfo in exchanges) {
                var requestPayloadPath =
                    DirectoryArchiveHelper.GetContentRequestPath(baseDirectory.FullName, exchangeInfo.Id);

                var responsePayloadPath =
                    DirectoryArchiveHelper.GetContentResponsePath(baseDirectory.FullName, exchangeInfo.Id);

                if (!connectionInfos.TryGetValue(exchangeInfo.ConnectionId, out var connectionInfo))
                    continue;

                await DumpExchange(zipArchive, ++sessionId, exchangeInfo, connectionInfo, max,
                    requestPayloadPath, responsePayloadPath);
            }
        }

        private static async Task WriteRequest(
            RequestHeaderInfo requestHeaderInfo,
            string payloadPath, Stream output, int exchangeId)
        {
            await using (var streamWriter = new StreamWriter(output, new UTF8Encoding(false), 1024 * 8, true)) {
                await streamWriter.WriteAsync($"{requestHeaderInfo.Method} {requestHeaderInfo.Path} HTTP/1.1\r\n");
                await streamWriter.WriteAsync($"Host: {requestHeaderInfo.Authority}\r\n");

                foreach (var header in requestHeaderInfo.Headers.Where(h => h.Forwarded)) {
                    if (header.Name.Span.StartsWith(":"))
                        continue;

                    await streamWriter.WriteAsync($"{header.Name}: {header.Value}\r\n");
                }

                await streamWriter.WriteAsync($"exchange-id: {exchangeId}\r\n");

                await streamWriter.WriteAsync("\r\n");
            }

            var payloadFileInfo = new FileInfo(payloadPath);

            if (payloadFileInfo.Exists && payloadFileInfo.Length > 0) {
                await using var bodyStream = File.OpenRead(payloadPath);
                await bodyStream.CopyToAsync(output);

                await bodyStream.FlushAsync();
            }
        }

        private static async Task WriteResponse(
            ResponseHeaderInfo responseHeaderInfo,
            string payloadPath, Stream output)
        {
            await using (var streamWriter = new StreamWriter(output, new UTF8Encoding(false), 1024 * 8, true)) {
                var statusCodeString = ((HttpStatusCode) responseHeaderInfo.StatusCode).ToString();

                await streamWriter.WriteAsync($"HTTP/1.1 {responseHeaderInfo.StatusCode} {statusCodeString}\r\n");

                foreach (var header in responseHeaderInfo.Headers.Where(h => h.Forwarded)) {
                    if (header.Name.Span.StartsWith(":"))
                        continue;

                    if (header.Name.Span.StartsWith("transfert-encoding"))
                        continue;

                    await streamWriter.WriteAsync($"{header.Name}: {header.Value}\r\n");
                }

                await streamWriter.WriteAsync("\r\n");
            }

            var payloadFileInfo = new FileInfo(payloadPath);

            if (payloadFileInfo.Exists && payloadFileInfo.Length > 0) {
                await using var bodyStream = File.OpenRead(payloadPath);
                await bodyStream.CopyToAsync(output);

                await bodyStream.FlushAsync();
            }
        }

        private static async Task DumpExchange(
            ZipArchive zipArchive, int sessionId,
            ExchangeInfo exchange,
            ConnectionInfo connectionInfo,
            int maxNumberId, string requestBodyPath, string responseBodyPath)
        {
            var requestEntry = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_c.txt");

            using (var requestStream = requestEntry.Open()) {
                await WriteRequest(exchange.RequestHeader, requestBodyPath, requestStream, exchange.Id);
            }

            var sessionEntry = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_m.xml");

            using (var sessionStream = sessionEntry.Open()) {
                var bodyLength = 0L;

                if (File.Exists(responseBodyPath))
                    bodyLength = new FileInfo(responseBodyPath).Length;

                WriteSessionContent(exchange, connectionInfo, sessionId, bodyLength, sessionStream);
            }

            if (exchange.ResponseHeader != null) {
                var response = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_s.txt");

                await using (var responseStream = response.Open()) {
                    await WriteResponse(exchange.ResponseHeader, responseBodyPath, responseStream);
                }
            }
        }

        private static void WriteSessionContent(
            ExchangeInfo exchange, ConnectionInfo connectionInfo, int sessionId, long bodyLength,
            Stream outStream)
        {
            using (var writer = XmlWriter.Create(outStream, new XmlWriterSettings {
                       Indent = true
                   })) {
                var flags = SazFlags.IsHttps | SazFlags.RequestStreamed | SazFlags.ResponseStreamed
                            | SazFlags.ImportedFromOtherTool;

                writer.WriteStartElement("Session");
                writer.WriteAttributeString("SID", sessionId.ToString());
                writer.WriteAttributeString("BitFlags", ((int) flags).ToString());

                writer.WriteStartElement("SessionTimers");

                writer.WriteAttributeString("ClientConnected",
                    exchange.Metrics.ReceivedFromProxy.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ClientBeginRequest",
                    exchange.Metrics.ReceivedFromProxy.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("GotRequestHeaders",
                    exchange.Metrics.ReceivedFromProxy.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ClientDoneRequest",
                    exchange.Metrics.ReceivedFromProxy.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("GatewayTime", "0");

                writer.WriteAttributeString("DNSTime", ((int)
                        (connectionInfo.DnsSolveEnd - connectionInfo.DnsSolveStart).TotalMilliseconds)
                    .ToString());

                writer.WriteAttributeString("TCPConnectTime", "0");

                if (connectionInfo.SslNegotiationEnd != default &&
                    connectionInfo.SslNegotiationStart != default) {
                    writer.WriteAttributeString("HTTPSHandshakeTime",
                        ((int) (connectionInfo.SslNegotiationEnd - connectionInfo.SslNegotiationStart)
                            .TotalMilliseconds)
                        .ToString());
                }

                writer.WriteAttributeString("ServerConnected",
                    connectionInfo.TcpConnectionOpened.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("FiddlerBeginRequest",
                    exchange.Metrics.RequestHeaderSending.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ServerGotRequest",
                    exchange.Metrics.RequestHeaderSent.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ServerBeginResponse",
                    exchange.Metrics.ResponseHeaderStart.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("GotResponseHeaders",
                    exchange.Metrics.ResponseHeaderEnd.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ServerDoneResponse",
                    exchange.Metrics.ResponseBodyEnd.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ClientBeginResponse",
                    exchange.Metrics.ResponseBodyEnd.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ClientDoneResponse",
                    exchange.Metrics.ResponseBodyEnd.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteEndElement();

                writer.WriteStartElement("PipeInfo");
                writer.WriteAttributeString("Streamed", "true");
                writer.WriteEndElement();

                writer.WriteStartElement("SessionFlags");

                void WriteSessionFlag(string name, string value)
                {
                    writer.WriteStartElement("SessionFlag");
                    writer.WriteAttributeString("N", name);
                    writer.WriteAttributeString("V", value);
                    writer.WriteEndElement();
                }

                WriteSessionFlag(
                    "x-responsebodytransferlength",
                    exchange.Metrics.TotalReceived.ToString());

                WriteSessionFlag(
                    "x-egressport",
                    connectionInfo.LocalPort.ToString());

                WriteSessionFlag(
                    "x-autoauth",
                    "(default)");

                WriteSessionFlag(
                    "x-clientport",
                    exchange.Metrics.DownStreamClientPort.ToString());

                WriteSessionFlag(
                    "x-clientip",
                    exchange.Metrics.DownStreamClientAddress ?? "");

                WriteSessionFlag(
                    "x-hostip",
                    connectionInfo.RemoteAddress ?? "");

                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.Flush();
            }
        }
    }

    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter<SazFlags>))]
    public enum SazFlags
    {
        None = 0,
        IsHttps = 1,
        RequestStreamed = 32,
        ResponseStreamed = 64,
        LoadedFromSaz = 512,
        ImportedFromOtherTool = 1024,
        SentToGateway = 2048, // 0x00000800
        IsBlindTunnel = 4096, // 0x00001000
        ResponseBodyDropped = 131072, // 0x00020000
        IsWebSocketTunnel = 262144, // 0x00040000
        RequestBodyDropped = 1048576 // 0x00100000
    }
}
