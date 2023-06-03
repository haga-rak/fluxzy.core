// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net;
using Fluxzy.Core;

namespace Fluxzy.Clients.Mock
{
    public class MockedResponseContent : PreMadeResponse
    {
        public MockedResponseContent(int statusCode, BodyContent bodyContent)
        {
            StatusCode = statusCode;
            BodyContent = bodyContent;
        }

        public int StatusCode { get; }

        public BodyContent BodyContent { get; }

        public override string GetFlatH11Header(Authority authority)
        {
            // TODO : introduce length and content encoding 

            var header =
                $"HTTP/1.1 {StatusCode} {((HttpStatusCode) StatusCode).ToString()}\r\n"
                + $"Host: {authority.HostName}:{authority.Port}\r\n";

            var bodyContentLength = BodyContent.GetLength();

            if (bodyContentLength > 0)
                header += $"Content-length: {bodyContentLength}\r\n";

            if (!string.IsNullOrWhiteSpace(BodyContent.Mime))
                header += $"Content-type: {BodyContent.Mime}\r\n";

            foreach (var extraHeader in BodyContent.Headers) {
                header += $"{extraHeader.Key}: {extraHeader.Value}\r\n";
            }

            header += "\r\n";

            return header;
        }

        public override Stream ReadBody(Authority authority)
        {
            return BodyContent.GetStream();
        }
    }
}
