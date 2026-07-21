// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

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

                var formattedValue = Format(context.ResponseBodyText);

                return new ResponseJsonResult(ResultTitle, formattedValue);
            }
            catch (Exception e) {
                if (e is FormatException || e is JsonException)
                    return null;

                throw;
            }
        }

        internal static string Format(string rawJson)
        {
            using var document = JsonDocument.Parse(rawJson);

            var outStream = new MemoryStream();

            // The relaxed encoder keeps non ASCII characters readable instead of \uXXXX,
            // safe here as the output is displayed as plain text, never as HTML
            using (var jsonWriter = new Utf8JsonWriter(outStream, new JsonWriterOptions {
                       Indented = true,
                       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                   })) {
                document.WriteTo(jsonWriter);
            }

            var formattedValue = Encoding.UTF8.GetString(outStream.GetBuffer(), 0, (int) outStream.Length);

            return UnescapeSurrogatePairs(formattedValue);
        }

        // JavaScriptEncoder allow lists only cover the basic multilingual plane,
        // characters above U+FFFF stay escaped by the writer and are restored here.
        // The lookbehind requires an even number of preceding backslashes so that
        // a literal backslash followed by "uXXXX" text is never decoded.
        private static readonly Regex SurrogatePairRegex = new(
            @"(?<=(?:^|[^\\])(?:\\\\)*)\\u(?<high>[Dd][89ABab][0-9A-Fa-f]{2})\\u(?<low>[Dd][C-Fc-f][0-9A-Fa-f]{2})",
            RegexOptions.Compiled);

        private static string UnescapeSurrogatePairs(string formattedJson)
        {
            return SurrogatePairRegex.Replace(formattedJson, match => {
                var high = (char) ushort.Parse(match.Groups["high"].Value, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);

                var low = (char) ushort.Parse(match.Groups["low"].Value, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);

                return new string(new[] { high, low });
            });
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
