using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Echoes
{
    //internal class HttpMessageHeaderParser
    //{
    //    internal class RequestLine
    //    {
    //        public string ForwardableRequestLine => $"{Method} {Uri?.PathAndQuery} {RawVersion}";
            
    //        public string Method { get; set; }

    //        public Uri Uri { get; set; }
            
    //        public HrmVersion Version { get; set; }

    //        public string RawVersion { get; set; }

    //        public string RawRequestLine { get; set; }

    //    }

    //    internal class ResponseLine
    //    {
    //        public string ForwardableRequestLine { get; set; }

    //        public int StatusCode { get; set; }

    //        public HrmVersion Version { get; set; }
    //    }

    //    private static readonly Encoding DefaultEncoding = Encoding.ASCII;

    //    private static (string Name, string Value) ParseHeader(string line)
    //    {
    //        var columnIndex = line.IndexOf(':');

    //        if (columnIndex < 0)
    //            return (null, null);

    //        var headerName = line.Substring(0, columnIndex)
    //            .Trim(); //  RFC 7230

    //        var headerValue = line.Substring(columnIndex + 1).Trim();

    //        return (headerName, headerValue);
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="firstLine"></param>
    //    /// <returns></returns>
    //    private static (RequestLine Result, string ErrorMessage) ParseRequestFirstLine(string firstLine)
    //    {
    //        var tab = firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    //        if (tab.Length != 3)
    //        {
    //            return (null, "Request-line invalid");
    //        }

    //        var method = tab[0];
    //        var rawUrl = tab[1];

    //        if (string.Equals(method, "CONNECT", StringComparison.OrdinalIgnoreCase))
    //        {
    //            rawUrl = $"https://{rawUrl}";
    //        }

    //        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uriResult))
    //        {
    //            if (uriResult.Scheme != "http" && uriResult.Scheme != "https")
    //            {
    //                uriResult = null; // MUST be a relative path
    //            }
    //        }
            
    //        var result = new RequestLine();

    //        result.Version = HttpMessageVersionExtensions.GetVersion(tab[2]);

    //        if (result.Version == HrmVersion.Unknown)
    //        {
    //            return (null, "Unknown HTTP Protocol");
    //        }

    //        result.RawRequestLine = rawUrl;
    //        result.RawVersion = tab[2];

    //        result.Uri = uriResult; 
    //        result.Method = method;

    //        return (result, null); 
    //    }

    //    private static (ResponseLine Result, string ErrorMessage) ParseResponseFirstLine(string firstLine)
    //    {
    //        var tab = firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    //        if (tab.Length < 2)
    //        {
    //            return (null, "Status Code Not Sent"); 
    //        }

    //        var version = HttpMessageVersionExtensions.GetVersion(tab[0]);

    //        if (version == HrmVersion.Unknown)
    //        {
    //            return (null, "Unknown HTTP version");
    //        }

    //        if (!int.TryParse(tab[1], out var statusCode))
    //        {
    //            return (null, "Unknown HTTP Code"); 
    //        }

    //        return (new ResponseLine()
    //        {
    //            ForwardableRequestLine = firstLine,
    //            Version = version,
    //            StatusCode = statusCode
    //        }, null); 
    //    }

    //    public static Hrm BuildRequestMessage(byte[] rawHeader, string hostName, int port)
    //    {
    //        Hrm message = new Hrm() { FullOriginalHeader = rawHeader }; 

    //        using (var headerStream = new MemoryStream(rawHeader, false))
    //        {
    //            using (var streamReader = new StreamReader(headerStream, DefaultEncoding, false))
    //            {
    //                var firstLine = streamReader.ReadLine();

    //                if (firstLine == null)
    //                {
    //                    message.AddError("Request-line invalid");

    //                    return message;
    //                }
                    
    //                var parseResult = ParseRequestFirstLine(firstLine);

    //                if (parseResult.Result == null)
    //                {
    //                    message.AddError(parseResult.ErrorMessage);
    //                    return message;
    //                }

    //                message.RawMethod = parseResult.Result.Method;
    //                message.Uri = parseResult.Result.Uri;
    //                message.Version = parseResult.Result.Version;

    //                if (string.Equals(message.RawMethod, "CONNECT", StringComparison.OrdinalIgnoreCase))
    //                {
    //                    message.IsTunnelConnectionRequested = true; 
    //                }

    //                string currentLine;
    //                bool isWebSocketRequest = false; 

    //                while ((currentLine = streamReader.ReadLine()) != null)
    //                {
    //                    var currentHeader = ParseHeader(currentLine);

    //                    if (string.IsNullOrWhiteSpace(currentHeader.Name))
    //                        continue;

    //                    if (!message.Headers.ContainsKey(currentHeader.Name))
    //                    {
    //                        message.Headers[currentHeader.Name] = new List<string>();
    //                    }

    //                    message.Headers[currentHeader.Name].Add(currentHeader.Value);

    //                    if (string.Equals(currentHeader.Name, "Connection", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        if (string.Equals(currentHeader.Value, "close", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            message.ShouldCloseConnection = true;
    //                        }

    //                        else
    //                        {
    //                            if (message.Version == HrmVersion.Http10)
    //                            {
    //                                message.ShouldCloseConnection = !string.Equals(currentHeader.Value,
    //                                    "Keep-alive", StringComparison.OrdinalIgnoreCase);
    //                            }
    //                        }
    //                    }
    //                    else
    //                    if (string.Equals(currentHeader.Name, "Host", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        message.DestinationHost = currentHeader.Value;
    //                    }
    //                    else
    //                    if (string.Equals(currentHeader.Name, "Content-Length", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        if (long.TryParse(currentHeader.Value, out var size))
    //                        {
    //                            message.ContentLength = size;
    //                            message.NoBody = message.ContentLength <= 0;
    //                        }
    //                    }
    //                    else
    //                    if (string.Equals(currentHeader.Name, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        if (string.Equals(currentHeader.Value, "chunked", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            message.IsChunkedTransfert = true;
    //                        }
    //                    }
    //                    else
    //                    if (string.Equals(currentHeader.Name, "Upgrade", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        if (string.Equals(currentHeader.Value, "websocket", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            isWebSocketRequest = true;
    //                        }
    //                    }

    //                    //forwardableStreamWriter.Write(currentLine + "\r\n");
    //                }

    //                if (string.IsNullOrWhiteSpace(message.DestinationHost))
    //                {
    //                    message.AddError("Host header not specified");
    //                }
                    
    //                if (message.Uri == null)
    //                {
    //                    if (port == 443)
    //                    {
    //                        message.Uri = parseResult.Result.Uri =
    //                            new Uri($"https://{hostName}{parseResult.Result.RawRequestLine}");
    //                    }
    //                    else
    //                    {
    //                        message.Uri = parseResult.Result.Uri =
    //                            new Uri($"https://{hostName}:{port}{parseResult.Result.RawRequestLine}");
    //                    }
    //                }
                   
    //                // HERE is the chance to transform the requestheader

    //                using (var forwardableStream = new MemoryStream())
    //                using (var forwardableWriter = new StreamWriter(forwardableStream, DefaultEncoding))
    //                {
    //                    forwardableWriter.Write(parseResult.Result.ForwardableRequestLine);
    //                    forwardableWriter.Write("\r\n");

    //                    foreach (var keyElement in message.Headers.ToList())
    //                    {
    //                        if (string.Equals(keyElement.Key, "Connection", StringComparison.OrdinalIgnoreCase)
    //                         && !isWebSocketRequest)
    //                        {
    //                            continue;
    //                        }

    //                        foreach (var value in keyElement.Value)
    //                        {
    //                            forwardableWriter.Write(keyElement.Key);
    //                            forwardableWriter.Write(": ");
    //                            forwardableWriter.Write(value);
    //                            forwardableWriter.Write("\r\n");
    //                        }
    //                    }

    //                    forwardableWriter.Write("\r\n");

    //                    forwardableWriter.Flush();
    //                    message.ForwardableHeader = forwardableStream.ToArray();

    //                    return message;
    //                }
    //            }
    //        }
    //    }

    //    public static Hpm BuildResponseMessage(Guid requestId, byte[] rawHeader, bool closeConnection = false)
    //    {
    //        Hpm message = new Hpm(requestId)
    //        {
    //            FullOriginalHeader = rawHeader,
    //        };
            
    //        var errors = new List<HttpProxyError>();

    //        message.Errors = errors;

    //        using (var forwardableStream = new MemoryStream())
    //        using (var forwardableStreamWriter = new StreamWriter(forwardableStream, DefaultEncoding))
    //        {
    //            using (var headerStream = new MemoryStream(rawHeader, false))
    //            {
    //                using (var streamReader = new StreamReader(headerStream, DefaultEncoding, false))
    //                {
    //                    var firstLine = streamReader.ReadLine();

    //                    if (firstLine == null)
    //                    {
    //                        message.AddError($"InvalidRequestHeader");
    //                        return message;
    //                    }

    //                    var parseResult = ParseResponseFirstLine(firstLine);

    //                    if (parseResult.Result == null)
    //                    {
    //                        message.AddError(parseResult.ErrorMessage);
    //                        return message;
    //                    }

    //                    message.StatusCode = parseResult.Result.StatusCode;
    //                    message.Version = parseResult.Result.Version;

    //                    forwardableStreamWriter.Write(parseResult.Result.ForwardableRequestLine);
    //                    forwardableStreamWriter.Write("\r\n");

    //                    string currentLine;

    //                    while ((currentLine = streamReader.ReadLine()) != null)
    //                    {
    //                        var currentHeader = ParseHeader(currentLine);

    //                        if (string.IsNullOrWhiteSpace(currentHeader.Name))
    //                            continue;

    //                        if (!message.Headers.ContainsKey(currentHeader.Name))
    //                        {
    //                            message.Headers[currentHeader.Name] = new List<string>();
    //                        }

    //                        message.Headers[currentHeader.Name].Add(currentHeader.Value);

    //                        if (string.Equals(currentHeader.Name, "Connection", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            if (string.Equals(currentHeader.Value, "close", StringComparison.OrdinalIgnoreCase))
    //                            {
    //                                message.ShouldCloseConnection = true;
    //                            }
    //                            else
    //                            {
    //                                if (message.Version == HrmVersion.Http10)
    //                                {
    //                                    message.ShouldCloseConnection = !string.Equals(currentHeader.Value,
    //                                        "Keep-alive", StringComparison.OrdinalIgnoreCase);
    //                                }
    //                            }

    //                            if (!string.Equals(currentHeader.Value, "upgrade", StringComparison.OrdinalIgnoreCase))
    //                                continue;
    //                        }

    //                        if (string.Equals(currentHeader.Name, "Content-Length", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            if (long.TryParse(currentHeader.Value, out var size))
    //                            {
    //                                message.ContentLength = size;
    //                            }
    //                        }
    //                        else
    //                        if (string.Equals(currentHeader.Name, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            if (string.Equals(currentHeader.Value, "chunked", StringComparison.OrdinalIgnoreCase))
    //                            {
    //                                message.IsChunkedTransfert = true;
    //                            }
    //                        }


    //                        if (string.Equals(currentHeader.Name, "Upgrade", StringComparison.OrdinalIgnoreCase)
    //                            && string.Equals(currentHeader.Value, "websocket", StringComparison.OrdinalIgnoreCase))
    //                        {
    //                            message.IsWebSocket = true; 
    //                        }

    //                        message.NoBody = message.ContentLength == 0 
    //                                         || message.StatusCode == 304 
    //                                         || message.StatusCode < 200 
    //                                         || message.StatusCode == 204
    //                                         || message.StatusCode == 205;

    //                        forwardableStreamWriter.Write(currentLine);
    //                        forwardableStreamWriter.Write("\r\n");
    //                    }

    //                    if (!message.NoBody && message.ContentLength <= 0 && !message.IsChunkedTransfert)
    //                    {
    //                        closeConnection = true;
    //                        message.CloseDownStreamConnection = true; 
    //                    }

    //                    if (closeConnection)
    //                    {
    //                        forwardableStreamWriter.Write("Connection: close");
    //                        forwardableStreamWriter.Write("\r\n");
    //                    }
    //                }
    //            }

    //            forwardableStreamWriter.Write("\r\n");
    //            forwardableStreamWriter.Flush();
    //            message.ForwardableHeader = forwardableStream.ToArray();

    //            return message;
    //        }
    //    }

    //}

    public enum HttpProxyErrorType
    {
        SemanticError = 1, 
        NetworkError,
        ClientError
    }

    public class HttpProxyError
    {
        public HttpProxyError(string message, HttpProxyErrorType ? errorType = null, string exceptionInformation = null)
        {
            Message = message;
            ExceptionInformation = exceptionInformation;

            Type = errorType ?? HttpProxyErrorType.SemanticError;
        }

        public HttpProxyErrorType Type { get; set; }

        public string Message { get; set; }

        public string ExceptionInformation { get; set; }
    }
}