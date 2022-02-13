namespace Echoes.Rules
{
    internal static class HeaderHelper
    {
        public static string BuildResponseHeader(int statusCode, int contentLength, string contentType)
        {
            var header =
                $"HTTP/1.1 {statusCode} OK\r\n" +
                $"Content-length : {contentLength}\r\n" +
                $"Content-type : {contentType}\r\n" +
                $"Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                $"Pragma: no-cache\r\n" +
                $"Server: Echoes\r\n" +
                $"Expires: 0\r\n" +
                $"\r\n";

            return header;
        }
    }
}