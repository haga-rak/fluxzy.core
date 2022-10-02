// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers
{
    public class RequestJsonBodyProducer : IFormattingProducer<RequestJsonResult>
    {
        public string ResultTitle => "JSON";

        public RequestJsonResult? Build(ExchangeInfo exchangeInfo, FormattingProducerContext context)
        {
            var headers = exchangeInfo.GetRequestHeaders()?.ToList();

            if (headers == null)
                return null;

            var jsonContentTypeSpecified =
                headers.Any(h =>
                    h.Name.Span.Equals("Content-type", StringComparison.OrdinalIgnoreCase)
                    && h.Value.Span.Contains("json", StringComparison.OrdinalIgnoreCase));

            if (!jsonContentTypeSpecified)
                return null;

            if (context.RequestBody.IsEmpty)
                return null;

            try
            {
                var requestBodyBytes = context.RequestBody!;

                using var document = JsonDocument.Parse(requestBodyBytes);

                var outStream = new MemoryStream();

                using (var jsonWriter = new Utf8JsonWriter(outStream, new JsonWriterOptions()
                       {
                           Indented = true
                       }))
                {
                    document.WriteTo(jsonWriter);
                }

                var formattedValue = Encoding.UTF8.GetString(outStream.GetBuffer(), 0, (int)outStream.Length);

                var rawValue = Encoding.UTF8.GetString(requestBodyBytes.Span);

                return new RequestJsonResult(ResultTitle, rawValue, formattedValue);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }

    public class RequestJsonResult : FormattingResult
    {
        public RequestJsonResult(string title, string rawBody, string formattedBody) : base(title)
        {
            RawBody = rawBody;
            FormattedBody = formattedBody;
        }

        public string RawBody { get; }

        public string FormattedBody { get; }

    }
}