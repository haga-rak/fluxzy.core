// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    public class TransformRequestBodyAction : Action
    {
        public TransformRequestBodyAction(Func<TransformContext, IBodyReader, Task<BodyContent?>> transformFunction)
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
        public override FilterScope ActionScope { get; } = FilterScope.RequestHeaderReceivedFromClient;

        /// <summary>
        /// 
        /// </summary>
        public override string DefaultDescription { get; } = "FX(Text)";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange != null) {
                var transformContext = new TransformContext(context, exchange, connection);

                context.RegisterRequestBodySubstitution(
                    new TransformRequestSubstitution(this, exchange, transformContext,
                        TransformFunction, InputEncoding));
            }

            return default; 
        }
    }
}
