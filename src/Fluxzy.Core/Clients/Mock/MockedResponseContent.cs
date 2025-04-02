// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Fluxzy.Core;
using Fluxzy.Rules;

namespace Fluxzy.Clients.Mock
{
    public partial class MockedResponseContent : PreMadeResponse
    {
        public MockedResponseContent(int statusCode, BodyContent body)
        {
            StatusCode = statusCode;
            Body = body;
        }

        [PropertyDistinctive(Description = "The status code of the response")]
        public int StatusCode { get; set; }

        [JsonInclude]
        [PropertyDistinctive(Description = "Response headers", FriendlyType = "array of (name, value)")]
        public List<MockedResponseHeader> Headers { get; set; } = new();

        [PropertyDistinctive(Description = "Body content", Expand = true)]
        public BodyContent? Body { get; set; }

        public override string GetFlatH11Header(Authority authority, ExchangeContext? exchangeContext)
        {
            var builder = new StringBuilder();

            builder.Append($"HTTP/1.1 {StatusCode} {((HttpStatusCode) StatusCode).ToString()}\r\n");
            builder.Append($"Host: {authority.HostName}:{authority.Port}\r\n");

            if (exchangeContext != null && Body != null && Body.Text != null) {
                Body.Text = Body.Text.EvaluateVariable(exchangeContext);
            }

            var bodyContentLength = Body?.GetLength() ?? 0;

            if (bodyContentLength >= 0) {
                builder.Append($"Content-length: {bodyContentLength}\r\n");
            }

            if (Body != null && !Headers.Any(k => k.Name.Equals("content-type", StringComparison.OrdinalIgnoreCase))) {
                var shortCutContentType = Body.GetContentTypeHeaderValue();

                if (shortCutContentType != null) {
                    builder.Append($"Content-Type: {shortCutContentType}\r\n");
                }
            }

            foreach (var header in Headers) {
                builder.Append($"{header.Name}: {header.Value}\r\n");
            }

            builder.Append("\r\n");

            return builder.ToString();
        }

        public override Stream ReadBody(Authority authority)
        {
            return Body == null ? Stream.Null : Body.GetStream();
        }

        public string? GetHeaderValueOrDefault(string headerName, string? defaultValue = default)
        {
            return Headers
                   .FirstOrDefault(k => k.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase))?.Value ??
                   defaultValue;
        }
    }

    public class MockedResponseHeader
    {
        public MockedResponseHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [PropertyDistinctive(Description = "Header name")]
        public string Name { get; }

        [PropertyDistinctive(Description = "Header value")]
        public string Value { get; }
    }
}
