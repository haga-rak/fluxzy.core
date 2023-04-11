using System;
using System.IO;

namespace Fluxzy.Utils
{
    public static class ExchangeUtility
    {
        public static string GetRequestBodyFileNameSuggestion(IExchange exchange)
        {
            var url = exchange.FullUrl;

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var fileName = Path.GetFileName(uri.LocalPath);

                if (!string.IsNullOrWhiteSpace(fileName))
                {

                    if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
                    {
                        return fileName + "." + HeaderUtility.GetRequestSuggestedExtension(exchange);
                    }

                    return fileName;
                }
            }

            return $"exchange-request-{exchange.Id}.data";
        }

        public static string GetResponseBodyFileNameSuggestion(ExchangeInfo exchange)
        {
            var url = exchange.FullUrl;
            
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
                var fileName = Path.GetFileName(uri.LocalPath);

                if (!string.IsNullOrWhiteSpace(fileName)) {

                    if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
                    {
                        return fileName + "." + HeaderUtility.GetResponseSuggestedExtension(exchange);
                    }

                    return fileName; 
                }
            }

            return $"exchange-response-{exchange.Id}.data";
        }

        public static bool TryGetAuthorityInfo(string fullUrl, out string hostName, out int port, out bool secure)
        {
            hostName = string.Empty;
            port = 0; 

            if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri)) {
                return false; 
            }

            hostName = uri.Host;
            port = uri.Port;
            secure = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase); 
            return true;
        }
    }
}