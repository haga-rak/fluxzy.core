﻿// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Fluxzy.Extensions;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Formatters.Producers.Responses;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Fluxzy.Har
{
    public class HarSerializeRootModel
    {
        public HarLogModel Log { get; }

        public HarSerializeRootModel(HarLogModel log)
        {
            Log = log;
        }
    }

    public class HarLogModel
    {
        public string Version { get; set; } = "1.2";

        public HarCreator Creator { get; set; } = new("fluxzy", $"{FluxzyMetainformation.Version}", null);

        public object? Browser { get; set; } = null;

        public object[]? Pages { get; set; } = null;

        public List<HarEntry> Entries { get; set; } = new();

        public string? Comment { get; set; }

        public HarLogModel(IArchiveReader archiveReader,
            IEnumerable<ExchangeInfo> exchanges,
            Dictionary<int, ConnectionInfo> connections,
            HttpArchiveSavingSetting savingSetting)
        {
            Entries = exchanges.Select(s => new HarEntry(s,
                connections.ContainsKey(s.ConnectionId) ? connections[s.ConnectionId] : null,
                archiveReader,
                savingSetting)).ToList();
        }
    }

    public record HarCreator(string Name, string Version, string? Comment)
    {
        public string? Comment { get; set; } = Comment;
    }

    public class HarEntry
    {
        [JsonPropertyName("_exchangeId")]
        public int ExchangeId { get; }

        public DateTime StartDateTime { get; set; }

        public int Time { get; }

        public string? ServerIpAddress { get; }

        public string? Connection { get; }

        public HarCache? Cache { get; set; }

        public HarTimings Timings { get; }

        public HarEntryRequest Request { get; }

        public HarEntryResponse Response { get; }

        public string? Comment { get; set; }

        public HarEntry(ExchangeInfo exchangeInfo,
            ConnectionInfo? connectionInfo,
            IArchiveReader archiveReader,
            HttpArchiveSavingSetting savingSetting)
        {
            var producerContext = new ProducerContext(exchangeInfo, archiveReader, FormatSettings.Default);

            if (exchangeInfo.Metrics.ResponseBodyEnd != default)
                Time = (int)(exchangeInfo.Metrics.ResponseBodyEnd - exchangeInfo.Metrics.ReceivedFromProxy)
                    .TotalMilliseconds;

            ExchangeId = exchangeInfo.Id;
            StartDateTime = exchangeInfo.Metrics.ReceivedFromProxy;
            ServerIpAddress = connectionInfo?.RemoteAddress;
            Connection = connectionInfo?.Id.ToString();
            Timings = new HarTimings(exchangeInfo, connectionInfo);
            Cache = new HarCache();

            Request = new HarEntryRequest(producerContext, savingSetting);
            Response = new HarEntryResponse(producerContext, savingSetting);
        }
    }

    public class HarEntryRequest
    {
        public List<HarQueryString> QueryString { get; } = new();

        public string Method { get; }

        public string Url { get; }

        public string HttpVersion { get; }

        public List<HarCookie> Cookies { get; }

        public int HeaderSize { get; } = -1;

        public long BodySize { get; } = -1;

        public List<HarHeader> Headers { get; }

        public HarPostData PostData { get; }

        public string? Comment { get; }

        public HarEntryRequest(ProducerContext producerContext, HttpArchiveSavingSetting savingSetting)
        {
            var exchangeInfo = producerContext.Exchange;
            var archiveReader = producerContext.ArchiveReader;

            Method = exchangeInfo.Method;
            Url = exchangeInfo.FullUrl;
            HttpVersion = exchangeInfo.HttpVersion;

            Cookies = HttpHelper.ReadRequestCookies(exchangeInfo.RequestHeader.Headers)
                                .Select(c => new HarCookie(c)).ToList();

            if (HttpHelper.TryGetQueryStrings(Url, out var item))
                QueryString = item.Select(s => new HarQueryString(s)).ToList();

            HeaderSize = exchangeInfo.Metrics.RequestHeaderLength;

            BodySize = archiveReader.GetRequestBodyLength(exchangeInfo.Id);

            Headers = exchangeInfo.GetRequestHeaders().Select(s => new HarHeader(s)).ToList();
            PostData = new HarPostData(producerContext, savingSetting);
        }
    }

    public class HarEntryResponse
    {
        public int Status { get; }

        public string StatusText { get; }

        public string? HttpVersion { get; }

        public List<HarCookie> Cookies { get; } = new();

        public List<HarHeader> Headers { get; } = new();

        public HarContent Content { get; }

        public string? RedirectURL { get; }

        public int HeaderSize { get; }

        public long BodySize { get; }

        public string? Comment { get; set; }

        public HarEntryResponse(ProducerContext producerContext, HttpArchiveSavingSetting saveSetting)
        {
            Status = producerContext.Exchange.StatusCode;
            StatusText = ((HttpStatusCode)Status).ToString();
            HttpVersion = producerContext.Exchange.HttpVersion;

            var responseHeaders = producerContext.Exchange.GetResponseHeaders()?.ToList();

            if (responseHeaders != null)
            {
                Cookies = HttpHelper.ReadResponseCookies(responseHeaders)
                                    .Select(c => new HarCookie(c)).ToList();

                Headers = responseHeaders.Select(r => new HarHeader(r)).ToList();
            }

            RedirectURL = producerContext.Exchange.FullUrl;
            HeaderSize = producerContext.Exchange.Metrics.ResponseHeaderLength;
            BodySize = producerContext.ArchiveReader.GetResponseBodyLength(producerContext.Exchange.Id);

            Content = new HarContent(producerContext, saveSetting);
        }
    }

    public class HarCookie
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public string? Path { get; set; }

        public string? Domain { get; set; }

        public DateTime? Expires { get; set; }

        public bool? HttpOnly { get; set; }

        public bool? Secure { get; set; }

        public string? Comment { get; set; }

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
            Expires = cookieItem.Expired;
            HttpOnly = cookieItem.HttpOnly;
            Secure = cookieItem.Secure;
        }
    }

    public class HarHeader
    {
        public string Name { get; }

        public string Value { get; }

        public string? Comment { get; set; }

        public HarHeader(HeaderFieldInfo headerFieldInfo)
        {
            Name = headerFieldInfo.Name.ToString();
            Value = headerFieldInfo.Value.ToString();
        }
    }

    public class HarQueryString
    {
        public string Name { get; }

        public string Value { get; }

        public string? Comment { get; set; }

        public HarQueryString(QueryStringItem stringItem)
        {
            Name = stringItem.Name;
            Value = stringItem.Value;
        }
    }

    public class HarPostData
    {
        public string? MimeType { get; }

        public List<HarParams> Params { get; set; } = new();

        public string Text { get; } = string.Empty;

        public string? Comment { get; set; }

        public HarPostData(ProducerContext producerContext, HttpArchiveSavingSetting savingSetting)
        {
            var exchangeInfo = producerContext.Exchange;
            var archiveReader = producerContext.ArchiveReader;

            MimeType = exchangeInfo.GetResponseHeaderValue("Content-Type");

            var requestLength = producerContext.ArchiveReader.GetRequestBodyLength(exchangeInfo.Id);

            if (requestLength > 0 && savingSetting.Comply(requestLength))
            {
                var destBuffer = ArrayPool<byte>.Shared.Rent((int)requestLength);

                try
                {
                    using var requestBodyStream = archiveReader.GetRequestBody(exchangeInfo.Id);
                    var length = requestBodyStream?.FillArray(destBuffer);

                    if (length != null)
                    {
                        var content = new Span<byte>(destBuffer, 0, (int)length);
                        var isText = ArrayTextUtilities.IsText(content, (int)length);

                        if (isText)
                        {
                            Text = Encoding.UTF8.GetString(content);
                        }
                        else
                        {
                            Text = Convert.ToBase64String(content);
                            Comment = "base64";
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(destBuffer);
                }
            }

            // Check for FormUrlEncoded 
            // more DI here 
            var formUrlEncodedProducer = new FormUrlEncodedProducer();

            var formUrlEncodedResult = formUrlEncodedProducer.Build(exchangeInfo, producerContext);

            if (formUrlEncodedResult != null)
            {
                Params = formUrlEncodedResult.Items.Select(s => new HarParams(s)).ToList();

                return;
            }

            var multiPartContent = new MultipartFormContentProducer();

            var multiPartContentResult = multiPartContent.Build(exchangeInfo, producerContext);

            if (multiPartContentResult != null)
                Params = multiPartContentResult.Items.Select(i => new HarParams(i)).ToList();
        }
    }

    public class HarParams
    {
        public string Name { get; set; }

        public string? Value { get; set; }

        public string? FileName { get; set; }

        public string? ContentType { get; set; }

        public string? Comment { get; set; }

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
    }

    public class HarCache
    {
        public HarBeforeAfterRequest? BeforeRequest { get; set; }

        public HarBeforeAfterRequest? AfterRequest { get; set; }
    }

    public class HarContent
    {
        public long Size { get; set; }

        public long Compressing { get; set; }

        public string MimeType { get; set; }

        public string Text { get; set; }

        public string Encoding { get; set; }

        public string? Comment { get; set; }

        public HarContent(ProducerContext producerContext, HttpArchiveSavingSetting savingSetting)
        {
            var exchangeInfo = producerContext.Exchange;
            Size = producerContext.ArchiveReader.GetResponseBodyLength(exchangeInfo.Id);
            Compressing = Size;
            MimeType = producerContext.Exchange.GetResponseHeaderValue("content-type") ?? "application/octet-stream";

            if (savingSetting.Comply(Size))
            {
                var textContext = producerContext.IsTextContent;

                if (textContext)
                {
                    Text = producerContext.ResponseBodyText 
                           ?? string.Empty;
                }
            }
        }
    }

    public class HarBeforeAfterRequest
    {
        public DateTime Expires { get; set; }

        public DateTime LastAccess { get; set; }

        public string ETag { get; set; }

        public long HitCount { get; set; }

        public string? Comment { get; set; }
    }

    public class HarTimings
    {
        public double Blocked { get; } = -1;

        public double Dns { get; }

        public double Connect { get; }

        public double Send { get; }

        public double Wait { get; }

        public double Receive { get; }

        public double Ssl { get; }

        public string? Comment { get; set; }

        public HarTimings(ExchangeInfo exchangeInfo, ConnectionInfo? connectionInfo)
        {
            if (exchangeInfo.Metrics.RetrievingPool != default)
                Blocked = (exchangeInfo.Metrics.RetrievingPool - exchangeInfo.Metrics.ReceivedFromProxy)
                    .TotalMilliseconds;

            if (connectionInfo != null)
            {
                if (connectionInfo.DnsSolveEnd != default)
                    Dns = (connectionInfo.DnsSolveEnd - connectionInfo.DnsSolveStart)
                        .TotalMilliseconds;

                if (connectionInfo.SslNegotiationEnd != default)
                    Ssl = (connectionInfo.SslNegotiationEnd - connectionInfo.SslNegotiationStart)
                        .TotalMilliseconds;

                if (connectionInfo.TcpConnectionOpened != default)
                    Connect = (connectionInfo.TcpConnectionOpened - connectionInfo.TcpConnectionOpening)
                        .TotalMilliseconds;
            }

            if (exchangeInfo.Metrics.RequestBodySent != default)
                Send = (exchangeInfo.Metrics.RequestBodySent - exchangeInfo.Metrics.RequestHeaderSending)
                    .TotalMilliseconds;

            if (exchangeInfo.Metrics.ResponseHeaderStart != default)
                Wait = (exchangeInfo.Metrics.ResponseHeaderStart - exchangeInfo.Metrics.RequestBodySent)
                    .TotalMilliseconds;

            if (exchangeInfo.Metrics.ResponseBodyEnd != default)
                Receive = (int)(exchangeInfo.Metrics.ResponseBodyEnd - exchangeInfo.Metrics.ResponseHeaderEnd)
                    .TotalMilliseconds;
        }
    }
}
