// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;

namespace Fluxzy.Clients.DotNetBridge
{
    /// <summary>
    /// An HttpResponseMessage that uses fluxzy internals to send requests.
    /// </summary>
    public class FluxzyHttpResponseMessage : HttpResponseMessage
    {
        public FluxzyHttpResponseMessage(Exchange exchange)
            : base(ReadStatusCode(exchange.Response.Header!.HeaderFields, out _))
        {
            Exchange = exchange;

            Version = Version.Parse("2.0");

            Content = new StreamContent(exchange.Response.Body!);

            foreach (var headerField in exchange.Response.Header.HeaderFields) {
                if (headerField.Name.Span.StartsWith(":".AsSpan()))
                    continue;

                if (!Headers.TryAddWithoutValidation(headerField.Name.ToString(), headerField.Value.ToString()))
                    Content.Headers.TryAddWithoutValidation(headerField.Name.ToString(), headerField.Value.ToString());
            }
        }

        public Exchange Exchange { get; }

        private static HttpStatusCode ReadStatusCode(
            IEnumerable<HeaderField> headerFields,
            out Dictionary<ReadOnlyMemory<char>, List<ReadOnlyMemory<char>>> dictionaryMapping)
        {
            dictionaryMapping = headerFields
                                .GroupBy(h => h.Name, SpanCharactersIgnoreCaseComparer.Default)
                                .ToDictionary(t => t.Key,
                                    t => t.Select(r => r.Value).ToList(), SpanCharactersIgnoreCaseComparer.Default);

            var status = int.Parse(dictionaryMapping[":status".AsMemory()].First().Span);

            return (HttpStatusCode) status;
        }

        public override string ToString()
        {
            return Exchange.Request.Header.ToString();
        }
    }
}
