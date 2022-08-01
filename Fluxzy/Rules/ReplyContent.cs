using System;
using System.IO;
using System.Text;

namespace Echoes.Rules
{
    public class ReplyContent : IDisposable
    {
        private readonly byte[] _body;

        private ReplyContent(string rawHeaderContent, byte [] body)
        {
            _body = body;
            Header = rawHeaderContent;
            BodyLength = body.LongLength; 
        }
        
        private ReplyContent(string rawHeaderContent, long length)
        {
            Header = rawHeaderContent;
            BodyLength = length; 
        }

        public string Header { get; }

        public Stream GetBody()
        {
            return new MemoryStream(_body);
        }

        public long BodyLength { get;  }

        public static ReplyContent Create(int statusCode, byte [] content, string contentType)
        {
            var header = HeaderHelper.BuildResponseHeader(statusCode, content.Length, contentType);

            return new ReplyContent(header, content);
        }

        public static ReplyContent Create(int statusCode, string content, string contentType)
        {
            var bodyData = Encoding.UTF8.GetBytes(content ?? string.Empty);
            return Create(statusCode, bodyData, contentType);
        }

        public void Dispose()
        {
            GetBody()?.Dispose();
        }
    }
}