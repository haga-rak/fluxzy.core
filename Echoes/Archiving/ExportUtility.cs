using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Echoes.Core.Utils;

namespace Echoes
{
    public class ExportUtility
    {
        public static async Task ConvertToSazFile(string inputFileName, string outputFileName)
        {
            using (var archiveFile = EchoesArchiveFile.OpenRead(inputFileName))
            using (var fileStream = File.Create(outputFileName)) 
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create)) 
            {
                int sessionId = 1;
                
                var exchanges = (await archiveFile.ReadArchives().ConfigureAwait(false)).Exchanges;
                foreach (var exchange in exchanges)
                {
                    var max = (int) Math.Ceiling(Math.Log10(exchanges.Count));
                    await DumpExchange(zipArchive, sessionId++, exchange, max).ConfigureAwait(false);
                }
            }
        }

        private static async Task DumpExchange(ZipArchive zipArchive, int sessionId, HttpExchange exchange, int maxNumberId)
        {
            var requestEntry = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_c.txt");

            using (var requestStream = requestEntry.Open())
            {
                await requestStream.WriteAsyncNS2(exchange.RequestMessage.ForwardableHeader).ConfigureAwait(false); 
                await requestStream.WriteAsyncNS2(await exchange.RequestMessage.ReadBodyAsByteArray().ConfigureAwait(false)).ConfigureAwait(false); 
            }

            var sessionEntry = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_m.xml");

            using (var sessionStream = sessionEntry.Open())
            {
                WriteSessionContent(exchange, sessionId, sessionStream);
            }

            if (exchange.ResponseMessage != null)
            {
                var response = zipArchive.CreateEntry($"raw/{sessionId.ToString(new string('0', maxNumberId))}_s.txt");

                using (var responseStream = response.Open())
                {
                    await responseStream.WriteAsyncNS2(exchange.ResponseMessage.FullOriginalHeader).ConfigureAwait(false);
                    await responseStream.WriteAsyncNS2(await exchange.ResponseMessage.ReadBodyAsByteArray().ConfigureAwait(false)).ConfigureAwait(false);
                }
            }
        }

        private static void WriteSessionContent(HttpExchange exchange, int sessionId, Stream outStream)
        {
            using (var writer = XmlWriter.Create(outStream))
            {
                writer.WriteStartElement("Session");
                writer.WriteAttributeString("SID", sessionId.ToString());
                writer.WriteAttributeString("BitFlags", "81");

                writer.WriteStartElement("SessionTimers");

                writer.WriteAttributeString("ClientConnected",
                    exchange.RequestMessage.ClientConnected?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ClientBeginRequest",
                    exchange.RequestMessage.DownStreamStartSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("GotRequestHeaders",
                    exchange.RequestMessage.HeaderSentToUpStream?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ClientDoneRequest",
                    exchange.RequestMessage.DownStreamCompleteSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("GatewayTime", "0");
                writer.WriteAttributeString("DNSTime", "0");
                writer.WriteAttributeString("TCPConnectTime", "0");

                if (exchange.RequestMessage.SslConnectionEnd != null &&
                    exchange.RequestMessage.SslConnectionStart != null)
                {
                    writer.WriteAttributeString("HTTPSHandshakeTime", 
                       ((int) (exchange.RequestMessage.SslConnectionEnd.Value - exchange.RequestMessage.SslConnectionStart.Value).TotalMilliseconds).ToString());
                }

                writer.WriteAttributeString("ServerConnected",
                    exchange.ResponseMessage.ServerConnected?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("FiddlerBeginRequest",
                    exchange.RequestMessage.SendingHeaderToUpStream?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ServerGotRequest",
                    exchange.RequestMessage.BodySentToUpStream?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ServerBeginResponse",
                    exchange.ResponseMessage.UpStreamStartSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("GotResponseHeaders",
                    exchange.ResponseMessage.UpStreamCompleteSendingHeader?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");

                writer.WriteAttributeString("ServerDoneResponse",
                        exchange.ResponseMessage.UpStreamCompleteSendingBody?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");
                
                writer.WriteAttributeString("ClientBeginResponse",
                    exchange.ResponseMessage.UpStreamCompleteSendingBody?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00");
                
                writer.WriteAttributeString("ClientDoneResponse",
                        exchange.ResponseMessage.UpStreamCompleteSendingBody?.FormatWithLocalKind() ?? "0001-01-01T00:00:00.0000000+01:00") ;
                

                writer.WriteEndElement();

                writer.WriteStartElement("PipeInfo");
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
                    (exchange?.ResponseMessage.OnWireContentLength ?? 0).ToString());

                WriteSessionFlag(
                    "x-egressport", 
                    (exchange?.UpStreamEndPointInfo?.LocalPort ?? 0).ToString());

                WriteSessionFlag(
                    "x-autoauth", 
                    "(default)");

                WriteSessionFlag(
                    "x-clientport",
                    (exchange?.DownStreamEndPointInfo?.RemotePort ?? 0).ToString());

                WriteSessionFlag(
                    "x-clientip",
                    (exchange?.DownStreamEndPointInfo?.RemoteAddress ?? ""));

                WriteSessionFlag(
                    "x-hostip",
                    (exchange?.UpStreamEndPointInfo?.RemoteAddress ?? ""));

                
                writer.WriteEndElement();

                writer.WriteEndElement();

                writer.Flush();
            }
            
        }
    }


    internal static class DateTimeFormatHelper
    {
        public static string FormatWithLocalKind(this DateTime date)
        {
            var printable = DateTime.SpecifyKind(date, DateTimeKind.Local);
            return printable.ToString("o");
        }
    }
}
