// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Fluxzy.Core;
using Fluxzy.Utils;
using Fluxzy.Writers;

namespace Fluxzy.Readers
{
    public class HarImportEngine : IImportEngine
    {
        public bool IsFormat(string fileName)
        {
            return fileName.EndsWith(".har", StringComparison.OrdinalIgnoreCase);
        }

        public void WriteToDirectory(string fileName, string directory)
        {
            var directoryArchiveWriter = new DirectoryArchiveWriter(directory, null);
            directoryArchiveWriter.Init();

            using var inStream = File.OpenRead(fileName);

            var model = JsonSerializer.Deserialize<HarReadModel>(inStream, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

            Dictionary<string, ConnectionInfo> internalConnections = new();

            int onTheFlyId = 1;
            int connectionId = 1;
            int exchangeId = 1; 

            foreach (var entry in model.Log.Entries) {

                if (entry.Request == null)
                    continue;

                if (entry.Response == null)
                    continue;

                if (!ExchangeUtility.TryGetAuthorityInfo(entry.Request.Url,
                        out var hostName, out var port, out var secure)) {
                    continue; 
                }

                var received = entry.StartedDateTime;
                var poolReceived = received.AddMilliseconds((entry.Timings?.Blocked).NullIfNegative() ?? 0); 
                var dnsStart = poolReceived;
                var dnsEnd = poolReceived.AddMilliseconds((entry.Timings?.Dns).NullIfNegative() ?? 0);
                var connectStart = dnsEnd;
                var connectEnd = connectStart.AddMilliseconds((entry.Timings?.Connect).NullIfNegative() ?? 0);
                var sslStart = connectStart; 
                var sslEnd = sslStart.AddMilliseconds((entry.Timings?.Ssl).NullIfNegative() ?? 0);
                var sendStart = connectEnd;
                var sendEnd = sendStart.AddMilliseconds((entry.Timings?.Send).NullIfNegative() ?? 0);
                var waitStart = sendEnd;
                var waitEnd = waitStart.AddMilliseconds((entry.Timings?.Wait).NullIfNegative() ?? 0);
                var receiveStart = waitEnd;
                var receiveEnd = receiveStart.AddMilliseconds((entry.Timings?.Receive).NullIfNegative() ?? 0);

                var reusingConnection = true; 

                if (!internalConnections.TryGetValue(
                        entry.Connection ?? string.Empty, out var connection)) {
                    reusingConnection = false; 
                    internalConnections[(onTheFlyId++).ToString()] =
                        connection = new ConnectionInfo(
                            connectionId++, new AuthorityInfo(hostName, port, secure),
                            secure
                                ? new SslInfo(SslProtocols.None, null, null, null, null,
                                    entry.Request.HttpVersion, null!,
                                    HashAlgorithmType.None, CipherAlgorithmType.None, default, default, default)
                                : null, 1, dnsStart,
                            dnsEnd, connectStart, connectEnd, sslStart, sslEnd, 0, string.Empty,
                            entry.ServerIPAddress ?? string.Empty, entry.Request.HttpVersion
                        );

                    directoryArchiveWriter.Update(connection, CancellationToken.None);
                }

                var uri = new Uri(entry.Request.Url);


                var startDateTime = entry.StartedDateTime; 

                var exchange = new ExchangeInfo(exchangeId++, 
                    connection.Id, entry.Request.HttpVersion,
                    new RequestHeaderInfo(entry.Request.Method, entry.Request.Url, 
                        entry.Request.Headers.Select(s => new HeaderFieldInfo(s.Name, s.Value)).StripContentAlterationHeaders()),
                    new ResponseHeaderInfo(entry.Response.Status, entry.Response.Headers
                                               .Select(s => new HeaderFieldInfo(s.Name, s.Value)).StripContentAlterationHeaders(), true),
                    new ExchangeMetrics() {
                        ResponseHeaderLength = entry.Response.EffectiveHeaderSize, 
                        RequestHeaderLength = entry.Request.EffectiveHeaderSize,
                        DownStreamClientPort = default, 
                        CreateCertEnd = startDateTime, 
                        CreateCertStart = startDateTime,
                        ErrorInstant = default, 
                        DownStreamClientAddress = "", 
                        ReceivedFromProxy = received,
                        RemoteClosed = default, 
                        RequestBodySent = sendEnd, 
                        RequestHeaderSending = sendStart, 
                        RequestHeaderSent = sendEnd,
                        ResponseBodyEnd = receiveEnd, 
                        ResponseBodyStart = receiveStart,
                        ResponseHeaderStart = receiveStart,
                        ResponseHeaderEnd = receiveStart, 
                        TotalReceived = entry.Response.EffectiveHeaderSize + entry.Response.EffectiveBodySize,
                        TotalSent = entry.Request.EffectiveHeaderSize + entry.Request.EffectiveBodySize,
                        RetrievingPool = poolReceived, 
                        ReusingConnection = reusingConnection, 
                    },
                    entry.ServerIPAddress ?? "", false, null, null, false, new (), null, new(),
                    uri.Host, uri.Port, connection.SslInfo != null

                    );

                directoryArchiveWriter.Update(exchange, CancellationToken.None);

                // Writing requestBody 

                using var requestBodyStream = directoryArchiveWriter.CreateRequestBodyStream(exchange.Id);
                entry.Request.PostData.Write(requestBodyStream);

                // Writing responseBody
                using var responseBodyStream = directoryArchiveWriter.CreateResponseBodyStream(exchange.Id);
                entry.Response.Content.Write(responseBodyStream);
            }


        }
    }

    internal static class HeaderExtension
    {
        public static IEnumerable<HeaderFieldInfo> StripContentAlterationHeaders(
            this IEnumerable<HeaderFieldInfo> headerFieldInfos)
        {

            return headerFieldInfos.Where(s => 
                !s.Name.Span.Equals("content-encoding", StringComparison.OrdinalIgnoreCase) &&
                !s.Name.Span.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase));
        }
    }

    internal static class DoubleExtension
    {
        public static double? NullIfNegative(this double? value)
        {
            if (value < 0)
                return null;

            return value;
        }
    }

    public class HarReadModel
    {
        public HarReadLog Log { get; set; } = new();
    }

    public class HarReadLog
    {
        public string Version { get; set; } = string.Empty;

        public List<HarReadEntry> Entries { get; set; } = new();
    }

    public class HarReadEntry
    {
        public string?  Connection { get; set; }

        // ReSharper disable once InconsistentNaming
        public string ? ServerIPAddress { get; set; }

        public HarReadTiming ? Timings { get; set; }

        public HarReadRequest?  Request { get; set; }

        public HarReadResponse? Response { get; set; }

        public DateTime StartedDateTime { get; set; }

    }

    public class HarReadRequest
    {
        public string Method { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string HttpVersion { get; set; } = string.Empty;

        public List<HarReadHeader> Headers { get; set; } = new(); 

        public int HeadersSize { get; set; }

        public HarReadPostData PostData { get; set; } = new();

        [JsonIgnore]
        public int EffectiveHeaderSize {
            get
            {
                if (HeadersSize <= 0)
                    return Headers.Sum(s => s.Name.Length + s.Value.Length + 2) + 4; 

                return HeadersSize;
            }
        }


        public int BodySize { get; set; }


        [JsonIgnore]
        public int EffectiveBodySize {
            get
            {
                if (BodySize < 0)
                    return 0; 

                return BodySize;
            }
        }
    }

    public class HarReadPostData
    {
        public string MimeType { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public List<HarReadParam> Params { get; set; } = new();

        public string?  Comment { get; set; }

        public void Write(Stream stream)
        {
            if (Comment == "base64")
            {
                var bytes = Convert.FromBase64String(Text);
                stream.Write(bytes);
                return;
            }

            var streamWriter = new StreamWriter(stream);

            if (!string.IsNullOrEmpty(Text)) {
                streamWriter.Write(Text);
                streamWriter.Flush();
                return;
            }

            var first = true;

            foreach (var param in Params) {
                if (!first)
                    streamWriter.Write("&");

                first = false;

                streamWriter.Write(param.Name);
                streamWriter.Write("=");
                streamWriter.Write(param.Value);
            }

        }
    }

    public class HarReadParam
    {
        public string Name { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string? FileName { get; set; }

        public string? ContentType { get; set; }
    }

    public class HarReadHeader
    {
        public HarReadHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; } 

        public string Value { get; set; }
    }

    public class HarReadResponse
    {
        public int Status { get; set; }

        public string? StatusText { get; set; }

        public List<HarReadHeader> Headers { get; set; } = new();

        public HarReadResponseContent Content { get; set; } = new();

        public int HeadersSize { get; set; }

        [JsonIgnore]
        public int EffectiveHeaderSize
        {
            get
            {
                if (HeadersSize <= 0)
                    return Headers.Sum(s => s.Name.Length + s.Value.Length + 2) + 4;

                return HeadersSize;
            }
        }

        public int BodySize { get; set; }


        [JsonIgnore]
        public int EffectiveBodySize
        {
            get
            {
                if (BodySize < 0)
                    return 0;

                return BodySize;
            }
        }

    }

    public class HarReadResponseContent
    {
        public long Size { get; set; }

        public string ? Text { get; set; }

        public string ? MimeType { get; set; }

        public string ? Encoding { get; set; }

        public int ? Compression { get; set; }

        public void Write(Stream stream)
        {
            if (string.IsNullOrEmpty(Text))
                return; // Nothing to write 
            
            var isBase64 = Encoding == "base64";

            if (isBase64) {
                var bytes = Convert.FromBase64String(Text);
                stream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                var writer = new StreamWriter(stream);
                writer.Write(Text);
                writer.Flush();
            }
        }
    }

    public class HarReadTiming
    {

        public double Blocked { get; set;  } = -1;

        public double Dns { get; set; }

        public double Connect { get; set; }

        public double Send { get; set; }

        public double Wait { get; set; }

        public double Receive { get; set; }

        public double Ssl { get; set; }
    }
}
