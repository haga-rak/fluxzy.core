using System;
using System.IO;
using System.Text;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Fluxzy.Utils.Curl
{
    internal class CurlRequestConverter
    {
        public CurlRequestConverter()
        {
        }

        public CurlCommandResult BuildCurlRequest(
            IArchiveReader archiveReader, 
            ExchangeInfo exchange, CurlProxyConfiguration? configuration)
        {
            var result = new CurlCommandResult(configuration); 
            var fullUrl = exchange.FullUrl;

            result.AddArgument(fullUrl);

            // Setting up method 

            var method = exchange.Method;

            result.AddOption("-X", method.ToUpper());

            // Setting up headers 

            foreach (var requestHeader in exchange.GetRequestHeaders())
            {
                if (!requestHeader.Forwarded)
                    continue;

                if (requestHeader.Name.Span.StartsWith(":"))
                    continue; 
                
                result.AddOption("--header", $"{requestHeader.Name}: {requestHeader.Value}");
            }

            using var requestBodyStream = archiveReader.GetRequestBody(exchange.Id);

            if (requestBodyStream != null && requestBodyStream.CanSeek && requestBodyStream.Length > 0)
            {
                if (requestBodyStream.Length > (1024 * 8))
                {
                    // We put file on temp 
                    AddBinaryPayload(result, requestBodyStream);
                }
                else
                {
                    Span<byte> buffer = stackalloc byte[(int) requestBodyStream.Length];

                    requestBodyStream.ReadExact(buffer);

                    if (ArrayTextUtilities.IsText(buffer))
                    {
                        var bodyString = Encoding.UTF8.GetString(buffer);
                        result.AddOption("--data", bodyString);
                    }
                    else
                    {
                        AddBinaryPayload(result, requestBodyStream);
                    }
                }
            }

            return result; 
        }

        private void AddBinaryPayload(CurlCommandResult result, Stream requestBodyStream)
        {
            var fullPostPath = Path.Combine(CurlExportSetting.CurlPostDataTempPath,
                $"{result.Id}.bin");

            using var fileStream = File.Create(fullPostPath);

            requestBodyStream.CopyTo(fileStream);

            result.PostDataPath = fullPostPath;
            result.AddOption("--data-binary", fullPostPath);
        }
    }
}
