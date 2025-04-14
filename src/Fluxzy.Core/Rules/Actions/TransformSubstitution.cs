// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Extensions;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Rules.Actions
{
    internal abstract class TransformSubstitution : IStreamSubstitution
    {
        private readonly Action _source;
        private readonly Exchange _exchange;
        private readonly TransformContext _transformContext;
        private readonly Func<TransformContext, IBodyReader, Task<BodyContent?>> _transformFunction;
        private readonly Encoding? _inputEncoding;

        protected TransformSubstitution(Action source, Exchange exchange,
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

        protected abstract Encoding? GetOriginalContentEncoding(Exchange exchange);

        public async ValueTask<Stream> Substitute(Stream originalStream)
        {
            // detect exchange encoding 
            // var inputEncoding = (_inputEncoding ?? _exchange.GetResponseEncoding()) ?? Encoding.UTF8;
            var inputEncoding = (_inputEncoding ?? GetOriginalContentEncoding(_exchange)) ?? Encoding.UTF8;
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

    internal class TransformResponseSubstitution : TransformSubstitution
    {
        public TransformResponseSubstitution(Action source, Exchange exchange,
            TransformContext transformContext,
            Func<TransformContext, IBodyReader, Task<BodyContent?>> transformFunction,
            Encoding? inputEncoding) :
            base(source, exchange, transformContext, transformFunction, inputEncoding)
        {
        }

        protected override Encoding? GetOriginalContentEncoding(Exchange exchange)
        {
            return exchange.GetResponseEncoding();
        }
    }
    internal class TransformRequestSubstitution : TransformSubstitution
    {
        public TransformRequestSubstitution(Action source, Exchange exchange,
            TransformContext transformContext,
            Func<TransformContext, IBodyReader, Task<BodyContent?>> transformFunction,
            Encoding? inputEncoding) :
            base(source, exchange, transformContext, transformFunction, inputEncoding)
        {
        }

        protected override Encoding? GetOriginalContentEncoding(Exchange exchange)
        {
            return null;
        }
    }
}
