// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json.Serialization;
using Fluxzy.Core;
using Fluxzy.Rules;

namespace Fluxzy.Clients.Mock
{
    public class MockedResponseContent : PreMadeResponse
    {
        public MockedResponseContent(int statusCode, BodyContent body)
        {
            StatusCode = statusCode;
            Body = body;
        }

        [PropertyDistinctive(Description = "The status code of the response")]
        public int StatusCode { get; }

        [PropertyDistinctive(Description = "Body content", Expand = true)]
        public BodyContent Body { get; }

        [JsonInclude]
        [PropertyDistinctive(Description = "Key values containing extra headers")]
        public Dictionary<string, string> Headers { get; set; } = new();

        public override string GetFlatH11Header(Authority authority)
        {
            // TODO : introduce length and content encoding 

            var header =
                $"HTTP/1.1 {StatusCode} {((HttpStatusCode) StatusCode).ToString()}\r\n"
                + $"Host: {authority.HostName}:{authority.Port}\r\n";

            var bodyContentLength = Body.GetLength();

            if (bodyContentLength > 0)
                header += $"Content-length: {bodyContentLength}\r\n";

            if (!string.IsNullOrWhiteSpace(Body.Mime))
                header += $"Content-type: {Body.Mime}\r\n";

            foreach (var extraHeader in Headers) {
                header += $"{extraHeader.Key}: {extraHeader.Value}\r\n";
            }

            header += "\r\n";

            return header;
        }

        public override Stream ReadBody(Authority authority)
        {
            return Body.GetStream();
        }
    }
}
