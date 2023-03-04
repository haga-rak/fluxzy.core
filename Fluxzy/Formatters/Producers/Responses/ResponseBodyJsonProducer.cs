// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Fluxzy.Formatters.Producers.Responses
{
    public class ResponseBodyJsonProducer : IFormattingProducer<ResponseJsonResult>
    {
        public string ResultTitle => "JSON";

        public ResponseJsonResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            try {
                if (context.ResponseBodyText == null)
                    return null;

                using var document = JsonDocument.Parse(context.ResponseBodyText);

                var outStream = new MemoryStream();

                using (var jsonWriter = new Utf8JsonWriter(outStream, new JsonWriterOptions {
                           Indented = true
                       })) {
                    document.WriteTo(jsonWriter);
                }

                var formattedValue = Encoding.UTF8.GetString(outStream.GetBuffer(), 0, (int) outStream.Length);

                return new ResponseJsonResult(ResultTitle, formattedValue);
            }
            catch (Exception e) {
                if (e is FormatException || e is JsonException)
                    return null;

                throw;
            }
        }
    }

    public class ResponseJsonResult : FormattingResult
    {
        public ResponseJsonResult(string title, string formattedContent)
            : base(title)
        {
            FormattedContent = formattedContent;
        }

        public string FormattedContent { get; }
    }
}
