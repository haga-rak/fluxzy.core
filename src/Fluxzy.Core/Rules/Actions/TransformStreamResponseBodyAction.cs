// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    public class TransformStreamResponseBodyAction : Action
    {
        private readonly Func<Stream, Stream> _transformFunction;

        public TransformStreamResponseBodyAction(Func<Stream, Stream> transformFunction)
        {
            _transformFunction = transformFunction;
        }

        public override FilterScope ActionScope { get; } = FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription { get; } = "Transform body";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.RegisterResponseBodySubstitution(new TransformRawStreamSubstitution(_transformFunction));
            return default; 
        }
    }

    public class TransformRawStreamSubstitution : IStreamSubstitution
    {
        private readonly Func<Stream, Stream> _transformFunction;

        public TransformRawStreamSubstitution(Func<Stream, Stream> transformFunction)
        {
            _transformFunction = transformFunction;
        }

        public ValueTask<Stream> Substitute(Stream originalStream)
        {
            var result = _transformFunction(originalStream);

            // Dispose the original stream

            originalStream.Dispose();

            // Return the transformed stream
            return new ValueTask<Stream>(result);
        }
    }
}
