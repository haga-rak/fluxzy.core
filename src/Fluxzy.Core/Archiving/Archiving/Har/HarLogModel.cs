// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Extensions;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Formatters.Producers.Responses;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Fluxzy.Archiving.Har
{
    public class HarSerializeRootModel
    {
        public HarSerializeRootModel(HarLogModel log)
        {
            Log = log;
        }

        public HarLogModel Log { get; }
    }

    public class HarLogModel
    {
        public HarLogModel(
            IArchiveReader archiveReader,
            IEnumerable<ExchangeInfo> exchanges,
            Dictionary<int, ConnectionInfo> connections,
            HttpArchiveSavingSetting savingSetting)
        {
            Entries = exchanges
                      .OrderBy(e => e.Metrics.ReceivedFromProxy)
                      .ThenBy(e => e.Id)
                      .Select(s => new HarEntry(s,
                connections.ContainsKey(s.ConnectionId) ? connections[s.ConnectionId] : null,
                archiveReader,
                savingSetting)).ToList();
        }

        public string Version { get; set; } = "1.2";

        public HarCreator Creator { get; set; } = new("fluxzy", $"{FluxzyMetaInformation.Version}", null);

        public object? Browser { get; set; } = null;

        public object[]? Pages { get; set; } = null;

        public List<HarEntry> Entries { get; set; } = new();

        public string? Comment { get; set; }
    }

    public record HarCreator(string Name, string Version, string? Comment)
    {
        public string? Comment { get; set; } = Comment;
    }

    public class HarEntry
    {
        public HarEntry(
            ExchangeInfo exchangeInfo,
            ConnectionInfo? connectionInfo,
            IArchiveReader archiveReader,
            HttpArchiveSavingSetting savingSetting)
        {
            using var producerContext = new ProducerContext(exchangeInfo, archiveReader, FormatSettings.Default);

            if (exchangeInfo.Metrics.ResponseBodyEnd != default) {
                Time = (int) (exchangeInfo.Metrics.ResponseBodyEnd - exchangeInfo.Metrics.ReceivedFromProxy)
                    .TotalMilliseconds;
            }

            ExchangeId = exchangeInfo.Id;
            StartedDateTime = exchangeInfo.Metrics.ReceivedFromProxy;
            ServerIpAddress = connectionInfo?.RemoteAddress;
            Connection = connectionInfo?.Id.ToString();
            Timings = new HarTimings(exchangeInfo, connectionInfo);
            Cache = new HarCache();
            Comment = exchangeInfo.Comment;

            Request = new HarEntryRequest(producerContext, savingSetting);
            Response = new HarEntryResponse(producerContext, savingSetting);
        }

        [JsonPropertyName("_exchangeId")]
        public int ExchangeId { get; }

        public DateTime StartedDateTime { get; set; }

        public int Time { get; }

        [JsonPropertyName("serverIPAddress")]
        public string? ServerIpAddress { get; }

        public string? Connection { get; }

        public HarCache? Cache { get; set; }

        public HarTimings Timings { get; }

        public HarEntryRequest Request { get; }

        public HarEntryResponse Response { get; }

        public string? Comment { get; set; }
    }

    public class HarEntryRequest
    {
        public HarEntryRequest(ProducerContext producerContext, HttpArchiveSavingSetting savingSetting)
        {
            var exchangeInfo = producerContext.Exchange;
            var archiveReader = producerContext.ArchiveReader;

            Method = exchangeInfo.Method;
            Url = exchangeInfo.FullUrl;
            HttpVersion = exchangeInfo.HttpVersion;

            Cookies = HttpHelper.ReadRequestCookies(exchangeInfo.RequestHeader.Headers.Select(h => (GenericHeaderField)h))
                                .Select(c => new HarCookie(c)).ToList();

            if (HttpHelper.TryGetQueryStrings(Url, out var item))
                QueryString = item.Select(s => new HarQueryString(s)).ToList();

            HeadersSize = exchangeInfo.Metrics.RequestHeaderLength;

            BodySize = archiveReader.GetRequestBodyLength(exchangeInfo.Id);

            Headers = exchangeInfo.GetRequestHeaders().Select(s => new HarHeader(s)).ToList();

            if (BodySize > 0)
                PostData = new HarPostData(producerContext, savingSetting);
        }

        public List<HarQueryString> QueryString { get; } = new();

        public string Method { get; }

        public string Url { get; }

        public string HttpVersion { get; }

        public List<HarCookie> Cookies { get; }

        public int HeadersSize { get; } = -1;

        public long BodySize { get; } = -1;

        public List<HarHeader> Headers { get; }

        public HarPostData? PostData { get; }

        public string? Comment { get; }
    }

    public class HarEntryResponse
    {
        public HarEntryResponse(ProducerContext producerContext, HttpArchiveSavingSetting saveSetting)
        {
            Status = producerContext.Exchange.StatusCode;
            StatusText = Http11Constants.GetStatusLineOrNull(Status) ?? string.Empty;
            HttpVersion = producerContext.Exchange.HttpVersion;

            var responseHeaders = producerContext.Exchange.GetResponseHeaders()?.ToList();

            if (responseHeaders != null) {
                Cookies = HttpHelper.ReadResponseCookies(responseHeaders)
                                    .Select(c => new HarCookie(c)).ToList();

                Headers = responseHeaders.Select(r => new HarHeader(r)).ToList();
            }

            RedirectURL = producerContext.Exchange.GetResponseHeaderValue("Location") ?? string.Empty;
            HeadersSize = producerContext.Exchange.Metrics.ResponseHeaderLength;
            BodySize = producerContext.ArchiveReader.GetResponseBodyLength(producerContext.Exchange.Id);

            Content = new HarContent(producerContext, saveSetting);
        }

        public int Status { get; }

        public string StatusText { get; }

        public string? HttpVersion { get; }

        public List<HarCookie> Cookies { get; } = new();

        public List<HarHeader> Headers { get; } = new();

        public HarContent Content { get; }

        public string? RedirectURL { get; }

        public int HeadersSize { get; }

        public long BodySize { get; }

        public string? Comment { get; set; }
    }

    public class HarCookie
    {
        [JsonConstructor]
        public HarCookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public HarCookie(RequestCookie requestCookie)
        {
            Name = requestCookie.Name;
            Value = requestCookie.Value;
        }

        public HarCookie(SetCookieItem cookieItem)
        {
            Name = cookieItem.Name;
            Value = cookieItem.Value;
            Path = cookieItem.Path;
            Domain = cookieItem.Domain;
            Expires = cookieItem.Expired == default ? null : cookieItem.Expired;
            HttpOnly = cookieItem.HttpOnly;
            Secure = cookieItem.Secure;
        }

        public string Name { get; set; }

        public string Value { get; set; }

        public string? Path { get; set; }

        public string? Domain { get; set; }

        public DateTime? Expires { get; set; }

        public bool? HttpOnly { get; set; }

        public bool? Secure { get; set; }

        public string? Comment { get; set; }
    }

    public class HarHeader
    {
        public HarHeader(HeaderFieldInfo headerFieldInfo)
        {
            Name = headerFieldInfo.Name.ToString();
            Value = headerFieldInfo.Value.ToString();
        }

        public string Name { get; }

        public string Value { get; }

        public string? Comment { get; set; }
    }

    public class HarQueryString
    {
        public HarQueryString(QueryStringItem stringItem)
        {
            Name = stringItem.Name;
            Value = stringItem.Value;
        }

        public string Name { get; }

        public string Value { get; }

        public string? Comment { get; set; }
    }

    public class HarPostData
    {
        public HarPostData(ProducerContext producerContext, HttpArchiveSavingSetting savingSetting)
        {
            var exchangeInfo = producerContext.Exchange;

            MimeType = exchangeInfo.GetRequestHeaderValue("Content-Type") ?? "application/octet-stream";

            var content = producerContext.RequestBody.Span;

            if (content.Length > 0 && savingSetting.Comply(content.Length)) {
                if (ArrayTextUtilities.IsText(content, content.Length))
                    Text = Encoding.UTF8.GetString(content);
                else {
                    Text = Convert.ToBase64String(content);
                    Comment = "base64";
                    IsBase64 = true;
                }
            }

            // Check for FormUrlEncoded 
            // more DI here 
            var formUrlEncodedProducer = new FormUrlEncodedProducer();

            var formUrlEncodedResult = formUrlEncodedProducer.Build(exchangeInfo, producerContext);

            if (formUrlEncodedResult != null) {
                Params = formUrlEncodedResult.Items.Select(s => new HarParams(s)).ToList();

                return;
            }

            var multiPartContent = new MultipartFormContentProducer();

            var multiPartContentResult = multiPartContent.Build(exchangeInfo, producerContext);

            if (multiPartContentResult != null)
                Params = multiPartContentResult.Items.Select(i => new HarParams(i)).ToList();
        }

        public string? MimeType { get; }

        public List<HarParams> Params { get; set; } = new();

        public string Text { get; } = string.Empty;

        public string? Comment { get; set; }

        [JsonIgnore] // For internal use only 
        public bool IsBase64 { get; set; }
    }

    public class HarParams
    {
        public HarParams(FormUrlEncodedItem formUrlEncodedItem)
        {
            Name = formUrlEncodedItem.Key;
            Value = formUrlEncodedItem.Value;
        }

        public HarParams(MultipartItem multiPartItem)
        {
            Name = multiPartItem.Name ?? "";
            Value = multiPartItem.StringValue;
            FileName = multiPartItem.FileName;
            ContentType = multiPartItem.ContentType;
        }

        public string Name { get; set; }

        public string? Value { get; set; }

        public string? FileName { get; set; }

        public string? ContentType { get; set; }

        public string? Comment { get; set; }
    }

    public class HarCache
    {
        public HarBeforeAfterRequest? BeforeRequest { get; set; }

        public HarBeforeAfterRequest? AfterRequest { get; set; }
    }

    public class HarContent
    {
        public HarContent(ProducerContext producerContext, HttpArchiveSavingSetting savingSetting)
        {
            var exchangeInfo = producerContext.Exchange;
            var bodySize = producerContext.ArchiveReader.GetResponseBodyLength(exchangeInfo.Id);
            var decodedBody = producerContext.ResponseBodyContent;

            Size = decodedBody?.Length ?? bodySize;

            if (decodedBody != null && producerContext.CompressionInfo?.EncodingToken != null)
                Compression = Size - bodySize;

            MimeType = producerContext.Exchange.GetResponseHeaderValue("content-type") ?? "application/octet-stream";

            if (bodySize > 0 && savingSetting.Comply(bodySize)) {
                var textContext = producerContext.IsTextContent;

                if (textContext) {
                    Text = producerContext.ResponseBodyText
                           ?? string.Empty;
                }
                else {
                    Text = decodedBody != null
                        ? Convert.ToBase64String(decodedBody)
                        : producerContext.ArchiveReader.GetResponseBody(exchangeInfo.Id)?.ToBase64String(true);
                    Encoding = "base64";
                }
            }
        }

        public long Size { get; set; }

        public long? Compression { get; set; }

        [Obsolete("Use Compression instead")]
        [JsonIgnore]
        public long Compressing {
            get => Compression ?? 0;
            set => Compression = value;
        }

        public string MimeType { get; set; }

        public string? Text { get; set; }

        public string? Encoding { get; set; }

        public string? Comment { get; set; }
    }

    public class HarBeforeAfterRequest
    {
        public DateTime Expires { get; set; }

        public DateTime LastAccess { get; set; }

        public string? ETag { get; set; }

        public long HitCount { get; set; }

        public string? Comment { get; set; }
    }

    public class HarTimings
    {
        public HarTimings(ExchangeInfo exchangeInfo, ConnectionInfo? connectionInfo)
        {
            if (exchangeInfo.Metrics.RetrievingPool != default) {
                Blocked = (exchangeInfo.Metrics.RetrievingPool - exchangeInfo.Metrics.ReceivedFromProxy)
                    .TotalMilliseconds;
            }

            var openedConnection = connectionInfo != null
                                   && connectionInfo.TcpConnectionOpening >= exchangeInfo.Metrics.ReceivedFromProxy;

            if (connectionInfo != null && openedConnection) {
                if (connectionInfo.DnsSolveEnd != default) {
                    Dns = (connectionInfo.DnsSolveEnd - connectionInfo.DnsSolveStart)
                        .TotalMilliseconds;
                }

                if (connectionInfo.SslNegotiationEnd != default) {
                    Ssl = (connectionInfo.SslNegotiationEnd - connectionInfo.SslNegotiationStart)
                        .TotalMilliseconds;
                }

                if (connectionInfo.TcpConnectionOpened != default) {
                    Connect = (connectionInfo.TcpConnectionOpened - connectionInfo.TcpConnectionOpening)
                        .TotalMilliseconds;
                }
            }

            if (exchangeInfo.Metrics.RequestBodySent != default) {
                Send = (exchangeInfo.Metrics.RequestBodySent - exchangeInfo.Metrics.RequestHeaderSending)
                    .TotalMilliseconds;
            }

            if (exchangeInfo.Metrics.ResponseHeaderStart != default) {
                Wait = (exchangeInfo.Metrics.ResponseHeaderStart - exchangeInfo.Metrics.RequestBodySent)
                    .TotalMilliseconds;
            }

            if (exchangeInfo.Metrics.ResponseBodyEnd != default) {
                Receive = (int) (exchangeInfo.Metrics.ResponseBodyEnd - exchangeInfo.Metrics.ResponseHeaderEnd)
                    .TotalMilliseconds;
            }
        }

        public double Blocked { get; } = -1;

        public double Dns { get; } = -1;

        public double Connect { get; } = -1;

        public double Send { get; }

        public double Wait { get; }

        public double Receive { get; }

        public double Ssl { get; } = -1;

        public string? Comment { get; set; }
    }
}
