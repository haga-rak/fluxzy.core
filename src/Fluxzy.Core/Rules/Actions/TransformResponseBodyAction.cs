// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Extensions;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Rules.Actions
{
    public class TransformResponseBodyAction : Action
    {
        public TransformResponseBodyAction(Func<TransformContext, IBodyReader, Task<BodyContent?>> transformFunction)
        {
            TransformFunction = transformFunction;
        }

        /// <summary>
        /// Function that takes the transform context and the original content as a string and returns the new content as a string
        /// </summary>
        public Func<TransformContext, IBodyReader, Task<BodyContent?>> TransformFunction { get; }

        /// <summary>
        /// Encoding used to decode the response body, if null, taken from the response headers,
        /// if not found in the response headers, defaults to UTF8
        /// </summary>
        public Encoding? InputEncoding { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public override FilterScope ActionScope { get; } = FilterScope.ResponseHeaderReceivedFromRemote;

        /// <summary>
        /// 
        /// </summary>
        public override string DefaultDescription { get; } = "FX(Text)";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange != null && connection != null) {
                var transformContext = new TransformContext(context, exchange, connection);

                context.RegisterResponseBodySubstitution(
                    new TransformResponseSubstitution(this, exchange, transformContext,
                        TransformFunction, InputEncoding));
            }

            return default; 
        }
    }

    internal class TransformResponseSubstitution : IStreamSubstitution
    {
        private readonly Action _source;
        private readonly Exchange _exchange;
        private readonly TransformContext _transformContext;
        private readonly Func<TransformContext, IBodyReader, Task<BodyContent?>> _transformFunction;
        private readonly Encoding? _inputEncoding;

        public TransformResponseSubstitution(Action source, Exchange exchange,
            TransformContext transformContext,
            Func<TransformContext, IBodyReader, Task<BodyContent?>> transformFunction,
            Encoding? inputEncoding)
        {
            _source = source;
            _exchange = exchange;
            _transformContext = transformContext;
            _transformFunction = transformFunction;
            _inputEncoding = inputEncoding;
        }

        public async ValueTask<Stream> Substitute(Stream originalStream)
        {
            // detect exchange encoding 
            var inputEncoding = (_inputEncoding ?? _exchange.GetResponseEncoding()) ?? Encoding.UTF8;
            // read the original stream to a string
            
            var bodyReader = new InternalBodyReader(originalStream, inputEncoding);

            // transform the string
            var bodyContent = await _transformFunction(_transformContext, bodyReader);

            if (bodyContent == null) {
                if (bodyReader.Consumed)
                    throw new RuleExecutionFailureException(
                        $"Could not return original content as it's already consumed by the transformation function", _source);

                // ignore the transformation 
                return originalStream;
            }

            // convert the transformed string to a stream
            if (!bodyReader.Consumed) {
                await bodyReader.InnerStream.DrainAsync();
            }

            return bodyContent.Stream;
        }
    }

    public class TransformContext
    {
        public TransformContext(ExchangeContext exchangeContext, Exchange exchange, Connection connection)
        {
            ExchangeContext = exchangeContext;
            Exchange = exchange;
            Connection = connection;
        }

        public ExchangeContext ExchangeContext { get; }

        public Exchange Exchange { get;  }

        public Connection Connection { get; }
    }

    internal class InternalBodyReader : IBodyReader
    {
        private readonly Stream _innerStream;
        private readonly Encoding _inputEncoding;

        public InternalBodyReader(Stream innerStream, Encoding inputEncoding)
        {
            _innerStream = innerStream;
            _inputEncoding = inputEncoding;
        }

        public async Task<string> ConsumeAsString()
        {
            if (Consumed)
            {
                throw new InvalidOperationException("Already read");
            }

            Consumed = true;

            return await _innerStream.ReadToEndGreedyAsync(_inputEncoding);
        }

        public async Task<byte[]> ConsumeAsBytes()
        {
            if (Consumed) {
                throw new InvalidOperationException("Already read");

            }

            Consumed = true;

            using var memoryStream = new MemoryStream();
            await _innerStream.CopyToAsync(memoryStream).ConfigureAwait(false);

            return memoryStream.ToArray();
        }

        public Stream ConsumeAsStream()
        {
            if (Consumed)
            {
                throw new InvalidOperationException("Already read");
            }

            Consumed = true;
            return _innerStream;
        }

        public bool Consumed { get; private set; }

        internal Stream InnerStream => _innerStream;
    }

    public interface IBodyReader
    {
        Task<string> ConsumeAsString();

        Task<byte[]> ConsumeAsBytes();

        Stream ConsumeAsStream();

        bool Consumed { get; }
    }

    public class BodyContent
    {
        private readonly Stream _stream;

        public BodyContent(string content, Encoding? encoding = null)
        {
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(content);
            _stream = new MemoryStream(bytes);
        }

        public BodyContent(byte[] content, Encoding encoding)
        {
            _stream = new MemoryStream(content);
        }

        public BodyContent(Stream stream, Encoding encoding)
        {
            _stream = stream;
        }

        internal Stream Stream => _stream;

        // Add an implicit cast from string 

        public static implicit operator BodyContent(string content)
        {
            return new BodyContent(content);
        }

        // Add an implicit cast from byte[]
        public static implicit operator BodyContent(byte[] content)
        {
            return new BodyContent(content, Encoding.UTF8);
        }

        // Add an implicit cast from Stream
        public static implicit operator BodyContent(Stream stream)
        {
            return new BodyContent(stream, Encoding.UTF8);
        }
    }
}
