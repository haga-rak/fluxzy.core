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

        public RequestJsonResult? Build(ExchangeInfo exchangeInfo, FormattingProducerParam producerSetting,
            IArchiveReader archiveReader)
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

            var requestBodyStream = archiveReader.GetRequestBody(exchangeInfo.Id);

            if (requestBodyStream is not { CanSeek: true })
                return null;

            if (requestBodyStream.Length > producerSetting.MaxFormattableJsonLength)
                // Request body JSON exceed the maximum authorized 
                return null;

            var buffer = ArrayPool<byte>.Shared.Rent((int) requestBodyStream.Length);

            try
            {
                requestBodyStream.SeekableStreamToBytes(buffer);

                using var document = JsonDocument.Parse(buffer);

                var outStream = new MemoryStream();

                using (var jsonWriter = new Utf8JsonWriter(outStream, new JsonWriterOptions()
                       {
                           Indented = true
                       }))
                {
                    document.WriteTo(jsonWriter);
                }

                var formattedValue = Encoding.UTF8.GetString(outStream.GetBuffer(), 0, (int)outStream.Length);
                var rawValue = Encoding.UTF8.GetString(buffer);

                return new RequestJsonResult(ResultTitle, rawValue, formattedValue);
            }
            catch (FormatException)
            {
                return null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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