using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace Echoes
{
    public class SazPackager : IDirectoryPackager
    {
        public bool ShouldApplyTo(string fileName)
        {
            return
                fileName.EndsWith(".saz", StringComparison.CurrentCultureIgnoreCase) ;
        }

        public async Task Pack(string directory, Stream outputStream)
        {
            var directoryInfo = new DirectoryInfo(directory);

            if (!directoryInfo.Exists)
            {
                throw new InvalidOperationException("Directory does not exists"); 
            }

            var connectionInfos = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "connections"))
                .EnumerateFiles("*.json", SearchOption.AllDirectories).ToList();

            Dictionary<int, ConnectionInfo> _connectioninfos = new Dictionary<int, ConnectionInfo>();

            foreach (var connectionInfofile in connectionInfos)
            {
                if (!connectionInfofile.Name.StartsWith("con-") || connectionInfofile.Length == 0)
                    continue;

                if (
                    !int.TryParse(connectionInfofile.Name.Replace("con-", string.Empty).Replace(".json", string.Empty),
                        out var connectionId))
                {
                    continue;
                }

                using var stream = connectionInfofile.Open(FileMode.Open);

                var connectionInfo = await JsonSerializer.DeserializeAsync<ConnectionInfo>(
                    stream, GlobalArchiveOption.JsonSerializerOptions);

                _connectioninfos[connectionId] = connectionInfo;
            }


            var requestFiles =
                new DirectoryInfo(Path.Combine(directoryInfo.FullName, "exchanges"))
                    .EnumerateFiles("*.json", SearchOption.AllDirectories).ToList();

            int sessionId = 0;

            using var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create);
            var max = (int) Math.Ceiling(Math.Log10(requestFiles.Count));

            foreach (var requestFile in requestFiles)
            {
                if (!requestFile.Name.StartsWith("ex-") || requestFile.Length == 0)
                    continue;

                if (
                    !int.TryParse(requestFile.Name.Replace("ex-", string.Empty).Replace(".json", string.Empty), 
                        out var exchangeId))
                {
                    continue; 
                }

                using var stream =  requestFile.Open(FileMode.Open);

                var exchangeInfo = await JsonSerializer.DeserializeAsync<ExchangeInfo>(
                    stream, GlobalArchiveOption.JsonSerializerOptions);

                if (exchangeInfo == null)
                    continue;

                var requestPayloadPath = Path.Combine(directoryInfo.FullName, "contents", $"req-{exchangeId}.data");
                var responsePayloadPath = Path.Combine(directoryInfo.FullName, "contents", $"res-{exchangeId}.data");

                if (!_connectioninfos.TryGetValue(exchangeInfo.ConnectionId, out var connectionInfo))
                    continue; 

               // var connectionInfo = _connectioninfos[exchangeInfo.ConnectionId]; 

                await DumpExchange(zipArchive, ++sessionId, exchangeInfo, connectionInfo, max,
                    requestPayloadPath, responsePayloadPath);
            }
        }

        private static async Task WriteRequest(
            RequestHeaderInfo requestHeaderInfo, 
            string payloadPath, Stream output, int exchangeId)
        {
          
            await using (var streamWriter = new StreamWriter(output, new UTF8Encoding(false), 1024 * 8, true))
            {
                await streamWriter.WriteAsync($"{requestHeaderInfo.Method} {requestHeaderInfo.Path} HTTP/1.1\r\n");
                await streamWriter.WriteAsync($"Host: {requestHeaderInfo.Authority}\r\n");

                foreach (var header in requestHeaderInfo.Headers.Where(h => h.Forwarded))
                {
                    if (header.Name.Span.StartsWith(":"))
                        continue; 

                    await streamWriter.WriteAsync($"{header.Name}: {header.Value}\r\n");
                }
                await streamWriter.WriteAsync($"exchange-id: {exchangeId}\r\n");

                await streamWriter.WriteAsync($"\r\n");
            }

            var payloadFileInfo = new FileInfo(payloadPath);

            if (payloadFileInfo.Exists && payloadFileInfo.Length > 0)
            {
                await using var bodyStream = File.OpenRead(payloadPath);
                await bodyStream.CopyToAsync(output);

                await bodyStream.FlushAsync();

            }
        }

        private static async Task WriteResponse(
            ResponseHeaderInfo responseHeaderInfo, 
            string payloadPath, Stream output)
        {
            await using (var streamWriter = new StreamWriter(output, new UTF8Encoding(false), 1024 * 8, true))
            {
                var statusCodeString = ((HttpStatusCode) (responseHeaderInfo.StatusCode)).ToString();

                await streamWriter.WriteAsync($"HTTP/1.1 {responseHeaderInfo.StatusCode} {statusCodeString}\r\n");

                foreach (var header in responseHeaderInfo.Headers.Where(h => h.Forwarded))
                {
                    if (header.Name.Span.StartsWith(":"))
                        continue;

                    if (header.Name.Span.StartsWith("transfert-encoding"))
                        continue;

                    await streamWriter.WriteAsync($"{header.Name}: {header.Value}\r\n");
                }

                await streamWriter.WriteAsync($"\r\n");
            }

            var payloadFileInfo = new FileInfo(payloadPath);

            if (payloadFileInfo.Exists && payloadFileInfo.Length > 0)
            {
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

            using (var requestStream = requestEntry.Open())
            {
                await WriteRequest(exchange.RequestHeader, requestBodyPath, requestStream, exchange.Id); 
            }

            var sessionEntry = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_m.xml");

            using (var sessionStream = sessionEntry.Open())
            {
                var bodyLength = 0L;

                if (File.Exists(responseBodyPath))
                    bodyLength = new FileInfo(responseBodyPath).Length; 

                WriteSessionContent(exchange, connectionInfo, sessionId, bodyLength, sessionStream);
            }
            
            var response = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_s.txt");

            using (var responseStream = response.Open())
            {
                await WriteResponse(exchange.ResponseHeader, responseBodyPath, responseStream); 
            }
        }

        private static void WriteSessionContent(
            ExchangeInfo exchange, ConnectionInfo connectionInfo, int sessionId, long bodyLength,
            Stream outStream)
        {
            using (var writer = XmlWriter.Create(outStream, new XmlWriterSettings()
                   {
                       Indent = true
                   }))
            {

                var flags = SazFlags.IsHTTPS & SazFlags.RequestStreamed & SazFlags.ResponseStreamed
                    & SazFlags.ImportedFromOtherTool; 

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
                    connectionInfo.SslNegotiationStart != default)
                {
                    writer.WriteAttributeString("HTTPSHandshakeTime",
                       ((int)(connectionInfo.SslNegotiationEnd - connectionInfo.SslNegotiationStart).TotalMilliseconds).ToString());
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
                    (exchange.Metrics.TotalReceived).ToString());

                WriteSessionFlag(
                    "x-egressport",
                    (connectionInfo.LocalPort).ToString());

                WriteSessionFlag(
                    "x-autoauth",
                    "(default)");

                WriteSessionFlag(
                    "x-clientport",
                    (exchange.Metrics.LocalPort).ToString());

                WriteSessionFlag(
                    "x-clientip",
                    (exchange.Metrics.LocalAddress ?? ""));
                
                WriteSessionFlag(
                    "x-hostip",
                    (connectionInfo.RemoteAddress ?? ""));

                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.Flush();
            }

        }
    }


    [Flags]
    public enum SazFlags
    {
        None = 0,
        IsHTTPS = 1,
        RequestStreamed = 32,
        ResponseStreamed = 64,
        LoadedFromSAZ = 512,
        ImportedFromOtherTool = 1024,
        SentToGateway = 2048, // 0x00000800
        IsBlindTunnel = 4096, // 0x00001000
        ResponseBodyDropped = 131072, // 0x00020000
        IsWebSocketTunnel = 262144, // 0x00040000
        RequestBodyDropped = 1048576, // 0x00100000
    }


    //public class ExportUtility
    //{
    //    public static async Task ConvertToSazFile(string inputFileName, string outputFileName)
    //    {
    //        using (var archiveFile = EchoesArchiveFile.OpenRead(inputFileName))
    //        using (var fileStream = File.Create(outputFileName))
    //        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create))
    //        {
    //            int sessionId = 1;

    //            var exchanges = (await archiveFile.ReadArchives().ConfigureAwait(false)).Exchanges;
    //            foreach (var exchange in exchanges)
    //            {
    //                var max = (int)Math.Ceiling(Math.Log10(exchanges.Count));
    //                await DumpExchange(zipArchive, sessionId++, exchange, max).ConfigureAwait(false);
    //            }
    //        }
    //    }

    //    private static async Task DumpExchange(ZipArchive zipArchive, int sessionId, HttpExchange exchange, int maxNumberId)
    //    {
    //        var requestEntry = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_c.txt");

    //        using (var requestStream = requestEntry.Open())
    //        {
    //            await requestStream.WriteAsyncNS2(exchange.RequestMessage.ForwardableHeader).ConfigureAwait(false);
    //            await requestStream.WriteAsyncNS2(await exchange.RequestMessage.ReadBodyAsByteArray().ConfigureAwait(false)).ConfigureAwait(false);
    //        }

    //        var sessionEntry = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_m.xml");

    //        using (var sessionStream = sessionEntry.Open())
    //        {
    //            WriteSessionContent(exchange, sessionId, sessionStream);
    //        }

    //        if (exchange.ResponseMessage != null)
    //        {
    //            var response = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_s.txt");

    //            using (var responseStream = response.Open())
    //            {
    //                await responseStream.WriteAsyncNS2(exchange.ResponseMessage.FullOriginalHeader).ConfigureAwait(false);
    //                await responseStream.WriteAsyncNS2(await exchange.ResponseMessage.ReadBodyAsByteArray().ConfigureAwait(false)).ConfigureAwait(false);
    //            }
    //        }
    //    }

    //    private static void WriteSessionContent(HttpExchange exchange, int sessionId, Stream outStream)
    //    {
    //        using (var writer = XmlWriter.Create(outStream))
    //        {
    //            writer.WriteStartElement("Session");
    //            writer.WriteAttributeString("SID", sessionId.ToString());
    //            writer.WriteAttributeString("BitFlags", "81");

    //            writer.WriteStartElement("SessionTimers");

    //            writer.WriteAttributeString("ClientConnected",
    //                exchange.RequestMessage.ClientConnected?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("ClientBeginRequest",
    //                exchange.RequestMessage.DownStreamStartSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("GotRequestHeaders",
    //                exchange.RequestMessage.HeaderSentToUpStream?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("ClientDoneRequest",
    //                exchange.RequestMessage.DownStreamCompleteSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("GatewayTime", "0");
    //            writer.WriteAttributeString("DNSTime", "0");
    //            writer.WriteAttributeString("TCPConnectTime", "0");

    //            if (exchange.RequestMessage.SslConnectionEnd != null &&
    //                exchange.RequestMessage.SslConnectionStart != null)
    //            {
    //                writer.WriteAttributeString("HTTPSHandshakeTime",
    //                   ((int)(exchange.RequestMessage.SslConnectionEnd.Value - exchange.RequestMessage.SslConnectionStart.Value).TotalMilliseconds).ToString());
    //            }

    //            writer.WriteAttributeString("ServerConnected",
    //                exchange.ResponseMessage.ServerConnected?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("FiddlerBeginRequest",
    //                exchange.RequestMessage.SendingHeaderToUpStream?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("ServerGotRequest",
    //                exchange.RequestMessage.BodySentToUpStream?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("ServerBeginResponse",
    //                exchange.ResponseMessage.UpStreamStartSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("GotResponseHeaders",
    //                exchange.ResponseMessage.UpStreamCompleteSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("ServerDoneResponse",
    //                    exchange.ResponseMessage.UpStreamCompleteSendingBody?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("ClientBeginResponse",
    //                exchange.ResponseMessage.UpStreamCompleteSendingBody?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

    //            writer.WriteAttributeString("ClientDoneResponse",
    //                    exchange.ResponseMessage.UpStreamCompleteSendingBody?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");


    //            writer.WriteEndElement();

    //            writer.WriteStartElement("PipeInfo");
    //            writer.WriteEndElement();

    //            writer.WriteStartElement("SessionFlags");

    //            void WriteSessionFlag(string name, string value)
    //            {
    //                writer.WriteStartElement("SessionFlag");
    //                writer.WriteAttributeString("N", name);
    //                writer.WriteAttributeString("V", value);
    //                writer.WriteEndElement();
    //            }

    //            WriteSessionFlag(
    //                "x-responsebodytransferlength",
    //                (exchange?.ResponseMessage.OnWireContentLength ?? 0).ToString());

    //            WriteSessionFlag(
    //                "x-egressport",
    //                (exchange?.UpStreamEndPointInfo?.LocalPort ?? 0).ToString());

    //            WriteSessionFlag(
    //                "x-autoauth",
    //                "(default)");

    //            WriteSessionFlag(
    //                "x-clientport",
    //                (exchange?.DownStreamEndPointInfo?.RemotePort ?? 0).ToString());

    //            WriteSessionFlag(
    //                "x-clientip",
    //                (exchange?.DownStreamEndPointInfo?.RemoteAddress ?? ""));

    //            WriteSessionFlag(
    //                "x-hostip",
    //                (exchange?.UpStreamEndPointInfo?.RemoteAddress ?? ""));


    //            writer.WriteEndElement();

    //            writer.WriteEndElement();

    //            writer.Flush();
    //        }

    //    }
    //}

    internal static class DateTimeFormatHelper
    {
        public static string FormatWithLocalKind(this DateTime date)
        {
            if (date == DateTime.MinValue)
                return null;

            var printable = DateTime.SpecifyKind(date, DateTimeKind.Local);
            return printable.ToString("o");
        }
    }
}
