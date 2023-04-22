// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net;

namespace Fluxzy.Clients.Mock
{
    public class ReplyStreamContent : PreMadeResponse
    {
        public ReplyStreamContent(int statusCode, BodyContent bodyContent)
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

            if (!string.IsNullOrWhiteSpace(BodyContent.MimeType))
                header += $"Content-type: {BodyContent.MimeType}\r\n";

            header += "\r\n";

            return header;
        }

        public override Stream ReadBody(Authority authority)
        {
            return BodyContent.GetStream();
        }
    }
}
