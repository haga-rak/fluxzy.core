// Copyright © 2022 Haga Rakotoharivelo

using System.IO;
using System.Net;

namespace Fluxzy.Clients.Mock
{
    public class ReplyStreamContent : PremadeResponse
    {
        public ReplyStreamContent(int statusCode, BodyContent bodyContent)
        {
            StatusCode = statusCode;
            BodyContent = bodyContent;
        }

        public int StatusCode { get; set; }

        public BodyContent BodyContent { get; set; }
        
        public override string GetFlatH11Header(Authority authority)
        {
            // TODO : introduce length and content encoding 

            var header =
                @$"HTTP/1.1 {StatusCode} {((HttpStatusCode)StatusCode).ToString()}\r\n" 
              + @$"Host : {authority.HostName}:{authority.Port}\r\n"
              + @$"\r\n";

            return string.Empty; 
        }

        public override Stream ReadBody(Authority authority)
        {
            throw new System.NotImplementedException();
        }
    }
}