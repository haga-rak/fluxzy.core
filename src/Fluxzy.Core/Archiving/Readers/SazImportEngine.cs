// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Xml.Linq;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Utils;
using Fluxzy.Writers;

namespace Fluxzy.Readers
{
    internal class SazImportEngine : IImportEngine
    {
        public bool IsFormat(string fileName)
        {
            try {
                using var zipArchive = ZipFile.Open(fileName, ZipArchiveMode.Read);
                return  zipArchive.Entries.Any(r => r.FullName.StartsWith("raw/"));
            }
            catch {
                // ignore zip reading error 
                return false;
            }
        }

        public void WriteToDirectory(string fileName, string directory)
        {
            using var zipArchive = ZipFile.Open(fileName, ZipArchiveMode.Read);
            

            var directoryArchiveWriter = new DirectoryArchiveWriter(directory, null);

            directoryArchiveWriter.Init();

            var textEntries = zipArchive.Entries
                                        .Where(r => r.FullName.StartsWith("raw/") && r.Name.EndsWith("_c.txt"))
                                        .ToList();

            InternalParse(directoryArchiveWriter, textEntries, zipArchive);

            // Read all connectionInfo 
        }

        private static int GetId(string name)
        {
            var index = name.IndexOf('_');

            if (index == -1)
                return -1;

            if (!int.TryParse(name.AsSpan().Slice(0, index), out var id))
                return -1;

            return id;
        }

        private static void InternalParse(
            DirectoryArchiveWriter writer,
            IEnumerable<ZipArchiveEntry> textEntries, ZipArchive archive)
        {
            Dictionary<int, ConnectionInfo> connections = new();
            Dictionary<int, ExchangeInfo> exchanges = new();

            var connectionId = 1;
            var exchangeId = 1;

            foreach (var requestEntry in textEntries) {
                // Read file to \r\n

                if (!TryGetId(requestEntry.Name, out var id))
                    continue; // No id no party 

                var xmlEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith("_m.xml") &&
                                                                   GetId(e.Name) == id);

                if (xmlEntry == null)
                    continue; // We ignore that entry

                XElement element;

                using (var tempStream = xmlEntry.Open()) {
                    element = XElement.Load(tempStream);
                }

                var sid = element.GetSessionId();

                if (sid == 0)
                    continue;

                using var requestBodyStream = ReadHeaders(requestEntry, out var requestHeaders);

                if (requestBodyStream == null)
                    continue;

                var methodHeader = requestHeaders.FirstOrDefault(s => s.Name.Span.Equals(
                    Http11Constants.MethodVerb.Span, StringComparison.Ordinal));

                if (methodHeader.Value.Length == 0)
                    continue;

                var authorityHeader = requestHeaders.FirstOrDefault(s => s.Name.Span.Equals(
                    Http11Constants.AuthorityVerb.Span
                    , StringComparison.Ordinal));

                if (authorityHeader.Name.Length == 0)
                    continue;

                var isConnect = methodHeader.Value.Span.Equals("CONNECT", StringComparison.OrdinalIgnoreCase);
                
                var responseEntry = archive.Entries.FirstOrDefault(e =>
                    e.Name.EndsWith("_s.txt") &&
                    GetId(e.Name) == id);

                using var responseBodyStream = ReadHeaders(responseEntry, out var responseHeaders);

                var serverConnected = element.GetSessionTimersValue("ServerConnected")!.Value;
                var sslStart = serverConnected.AddMilliseconds(-element.GetSessionDurationValue("HTTPSHandshakeTime"));
                var tcpConnectStart = sslStart.AddMilliseconds(-element.GetSessionDurationValue("TCPConnectTime"));
                var dnsStart = tcpConnectStart.AddMilliseconds(-element.GetSessionDurationValue("DNSTime"));
                var received = element.GetSessionTimersValue("GotRequestHeaders")!.Value;

                if (isConnect) {
                    var connectRequestBody = requestBodyStream.ReadToEndGreedy();

                    if (string.IsNullOrWhiteSpace(connectRequestBody))
                        continue;

                    var responseBodyString = responseBodyStream?.ReadToEndGreedy() ?? null;

                    var flatAuthority = authorityHeader.Value.ToString();

                    if (!AuthorityUtility.TryParse(flatAuthority, out var host, out var port))
                        continue;

                    // Read flat body 

                    // Parse TLS Version 

                    var sslInfo = new SslInfo(
                        SazConnectBodyUtility.GetSslVersion(connectRequestBody) ?? SslProtocols.None,
                        SazConnectBodyUtility.GetCertificateIssuer(responseBodyString) ?? string.Empty,
                        SazConnectBodyUtility.GetCertificateIssuer(responseBodyString) ?? string.Empty,
                        SazConnectBodyUtility.GetCertificateCn(responseBodyString) ?? string.Empty,
                        SazConnectBodyUtility.GetCertificateCn(responseBodyString) ?? string.Empty,
                        "HTTP/1.1",
                        SazConnectBodyUtility.GetKeyExchange(responseBodyString) ?? string.Empty,
                        HashAlgorithmType.Sha384,
                        CipherAlgorithmType.Aes256, default, default, default
                    );
                    
                    var connectConnectionInfo = new ConnectionInfo(
                        connectionId++,
                        new AuthorityInfo(host!, port, true),
                        sslInfo,
                        1,
                        dnsStart,
                        tcpConnectStart,
                        tcpConnectStart,
                        sslStart,
                        sslStart,
                        serverConnected,
                        element.GetSessionFlagsAttributeAsInteger("x-egressport"),
                        "not set",
                        element.GetSessionFlagsAttributeAsString("x-hostip") ?? "not set",
                        "HTTP/1.1"
                    );

                    connections[sid] = connectConnectionInfo;
                    writer.Update(connectConnectionInfo, default);

                    continue;
                }

                // Check if plain has connect info 

                ConnectionInfo? connectionInfo = null;

                if (element.IsConnectionOpener()) {

                    // PLAIN http 
                    
                    var flatAuthority = authorityHeader.Value.ToString();

                    if (!AuthorityUtility.TryParse(flatAuthority, out var host, out var port)) {
                        host = flatAuthority;
                        port = 80; 
                    }

                    connectionInfo = new ConnectionInfo(
                        connectionId++,
                        new AuthorityInfo(host!, port, false),
                        null,
                        1,
                        dnsStart,
                        tcpConnectStart,
                        tcpConnectStart,
                        sslStart,
                        sslStart,
                        serverConnected,
                        element.GetSessionFlagsAttributeAsInteger("x-egressport"),
                        "not set",
                        element.GetSessionFlagsAttributeAsString("x-hostip") ?? "not set",
                        "HTTP/1.1"
                    );

                    connections[sid] = connectionInfo;
                    writer.Update(connectionInfo, default);
                }

                var newConnection = false; 

                if (connectionInfo == null) {
                    // get server pipe reuse 

                    var connectId = element.GetConnectId();

                    if (connectId == null)
                        continue; // We dont have a connect id here 

                    if (!connections.TryGetValue(connectId.Value, out connectionInfo))
                        continue; // ordering problem 

                    connectionInfo.RequestProcessed++;

                    newConnection = true; 
                }

                var agentInfoName = element.GetSessionFlagsAttributeAsString("x-processinfo") ?? "unspecified";

                var requestHeaderLength = requestHeaders.Sum(s => s.Name.Length + s.Value.Length);
                var responseHeaderLength = responseHeaders.Sum(s => s.Name.Length + s.Value.Length);

                var metrics = new ExchangeMetrics() {
                    DownStreamClientPort = element.GetSessionFlagsAttributeAsInteger("x-egressport"), 
                    CreateCertEnd = received,
                    CreateCertStart = received,
                    ErrorInstant = default, 
                    DownStreamClientAddress = "not set",
                    ReceivedFromProxy = received,
                    RemoteClosed = default, 
                    RequestHeaderLength = requestHeaderLength,
                    ResponseHeaderLength = responseHeaderLength,
                    RetrievingPool = received,
                    RequestHeaderSending = element.GetSessionTimersValue("FiddlerBeginRequest")! ?? default,
                    RequestHeaderSent = element.GetSessionTimersValue("ServerGotRequest")! ?? default,
                    RequestBodySent = element.GetSessionTimersValue("ServerGotRequest")! ?? default,
                    ResponseHeaderStart = element.GetSessionTimersValue("ServerBeginResponse")! ?? default,
                    ResponseHeaderEnd = element.GetSessionTimersValue("GotResponseHeaders")! ?? default,
                    ResponseBodyStart = element.GetSessionTimersValue("GotResponseHeaders")! ?? default,
                    ResponseBodyEnd = element.GetSessionTimersValue("ServerDoneResponse")! ?? default,
                    ReusingConnection = !newConnection,
                    TotalReceived = responseHeaderLength + element.GetSessionFlagsAttributeAsInteger("x-responsebodytransferlength"),
                    TotalSent = requestHeaderLength + element.GetSessionFlagsAttributeAsInteger("x-requestbodytransferlength"),
                };

                var exchangeInfo = new ExchangeInfo(
                    exchangeId++,
                    connectionInfo.Id,
                    "HTTP/1.1",
                    new RequestHeaderInfo(
                        new RequestHeader(requestHeaders), true),
                    new ResponseHeaderInfo(
                        new ResponseHeader(responseHeaders), true),
                    metrics,
                    connectionInfo.RemoteAddress!,
                    false,
                    null,
                    null,
                    false,
                    new List<WsMessage>(),
                    new Agent(agentInfoName.GetHashCode(), agentInfoName),
                    new List<ClientError>(),
                    connectionInfo.Authority.HostName,
                    connectionInfo.Authority.Port,
                    connectionInfo.SslInfo != null
                );

                // Fix header info 

                writer.Update(exchangeInfo, default);


                if (requestBodyStream.CanRead)
                    requestBodyStream
                        .CopyToThenDisposeDestination(writer.CreateRequestBodyStream(exchangeInfo.Id));

                if (responseBodyStream?.CanRead ?? false)
                    responseBodyStream
                        .CopyToThenDisposeDestination(writer.CreateResponseBodyStream(exchangeInfo.Id));
                
                // Read exchanges 
            }
        }

        private static Stream? ReadHeaders(ZipArchiveEntry? entry, out List<HeaderField> headers)
        {
            headers = new List<HeaderField>();

            if (entry == null)
                return null;

            int doubleCrLf;

            using (var tempStream = entry.Open()) {
                doubleCrLf = ReadUntilDoubleCrlLf(tempStream);
            }

            if (doubleCrLf < 0)
                return null;

            var stream = entry.Open();

            var buffer = ArrayPool<byte>.Shared.Rent(doubleCrLf);
            string requestHeaderString;

            try {
                //stream.ReadExact(buffer);

                if (!stream.ReadExact(buffer.AsSpan().Slice(0, doubleCrLf)))
                    return null;

                requestHeaderString = Encoding.UTF8.GetString(buffer, 0, doubleCrLf);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            headers = Http11Parser.Read(requestHeaderString.AsMemory()).ToList();

            var res = stream.DrainUntil(4); // SKIP DOUBLE CRLF

            return stream;
        }

        private static int ReadUntilDoubleCrlLf(Stream stream)
        {
            var searchStream = new SearchStream(stream, Http11Constants.DoubleCrLf);
            searchStream.Drain();

            return (int) (searchStream.Result?.OffsetFound ?? -1);
        }

        private static string? GetConnectBodyString(Stream stream, int offset)
        {
            stream.DrainUntil(offset + 4);

            using var streamReader = new StreamReader(stream);

            return streamReader.ReadToEnd();
        }

        private static bool TryGetId(string name, out int result)
        {
            result = -1;

            var endIndex = name.IndexOf('_');

            if (endIndex < 0)
                return false;

            return int.TryParse(name.Substring(0, endIndex), out result);
        }
    }
}
