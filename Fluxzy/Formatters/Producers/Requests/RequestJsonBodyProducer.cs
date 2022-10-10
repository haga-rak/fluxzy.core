// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Fluxzy.Formatters.Producers.Requests
{

    public class RequestJsonBodyProducer : IFormattingProducer<RequestJsonResult>
    {
        public string ResultTitle => "JSON";

        public RequestJsonResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            var headers = exchangeInfo.GetRequestHeaders()?.ToList();

            if (headers == null)
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

                var rawValue = context.RequestBodyText;

                return new RequestJsonResult(ResultTitle, rawValue, formattedValue);
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is JsonException)
                    return null;

                throw;
            }
        }
    }

    public class RequestJsonResult : FormattingResult
    {
        public RequestJsonResult(string title, string? rawBody, string formattedBody) : base(title)
        {
            RawBody = rawBody;
            FormattedBody = formattedBody;
        }

        public string? RawBody { get; }

        public string FormattedBody { get; }

    }
}