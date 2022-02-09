﻿// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Echoes.Encoding;
using Echoes.Encoding.Utils;

namespace Echoes.H2.DotNetBridge
{
    public class EchoesHttpResponseMessage : HttpResponseMessage
    {
        private readonly H2Message _message;

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

        public H2Message Message => _message;

        public Exchange Exchange { get; private set; }

        public EchoesHttpResponseMessage(H2Message message)
            : base(ReadStatusCode(message.HeaderFields, out _))
        {
            _message = message;

            Version = Version.Parse("2.0");

            Content = new StreamContent(message.ResponseStream);

            foreach (var headerField in message.HeaderFields)
            {
                if (headerField.Name.Span.StartsWith(":".AsSpan()))
                    continue;

                if (!Headers.TryAddWithoutValidation(headerField.Name.ToString(), headerField.Value.ToString()))
                {
                    Content.Headers.TryAddWithoutValidation(headerField.Name.ToString(), headerField.Value.ToString());
                }
            }
        }
        public EchoesHttpResponseMessage(Exchange exchange)
            : base(ReadStatusCode(exchange.Response.Header.HeaderFields, out _))
        {
            _message = null;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _message.Dispose();
        }

        public override string ToString()
        {
            return _message.Header; 
        }
    }
}