// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Echoes.Clients.H2.Encoder;
using Echoes.Clients.H2.Encoder.Utils;

namespace Echoes.Clients.DotNetBridge
{
    public class EchoesHttpResponseMessage : HttpResponseMessage
    {
        private static HttpStatusCode ReadStatusCode(IEnumerable<HeaderField> headerFields, 
            out Dictionary<ReadOnlyMemory<char>, List<ReadOnlyMemory<char>>> dictionaryMapping)
        {
            dictionaryMapping = headerFields
                .GroupBy(h => h.Name, SpanCharactersIgnoreCaseComparer.Default)
                .ToDictionary(t => t.Key,
                    t => t.Select(r => r.Value).ToList(), SpanCharactersIgnoreCaseComparer.Default);

            var status = int.Parse(dictionaryMapping[":status".AsMemory()].First().Span);
            

            return (HttpStatusCode)status;
        }

        public Exchange Exchange { get; }

        public EchoesHttpResponseMessage(Exchange exchange)
            : base(ReadStatusCode(exchange.Response.Header.HeaderFields, out _))
        {
            Exchange = exchange;
            
            Version = Version.Parse("2.0");

            Content = new StreamContent(exchange.Response.Body);

            foreach (var headerField in exchange.Response.Header.HeaderFields)
            {
                if (headerField.Name.Span.StartsWith(":".AsSpan()))
                    continue;

                if (!Headers.TryAddWithoutValidation(headerField.Name.ToString(), headerField.Value.ToString()))
                {
                    Content.Headers.TryAddWithoutValidation(headerField.Name.ToString(), headerField.Value.ToString());
                }
            }
        }

        public override string ToString()
        {
            return Exchange.Request.Header.ToString(); 
        }
    }
}