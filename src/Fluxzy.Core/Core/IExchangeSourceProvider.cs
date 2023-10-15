// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Rules;

namespace Fluxzy.Core
{
    /// <summary>
    /// An exchange source provider is responsible for reading exchanges from a stream.
    /// </summary>
    internal interface IExchangeSourceProvider
    {
        /// <summary>
        /// Called to init a first connection 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="contextBuilder"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer, 
            IExchangeContextBuilder contextBuilder,
            CancellationToken token);

        /// <summary>
        /// Read an exchange from the client stream
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="authority"></param>
        /// <param name="buffer"></param>
        /// <param name="options"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        ValueTask<Exchange?> ReadNextExchange(
            Stream inStream, Authority authority, RsBuffer buffer,
            IExchangeContextBuilder contextBuilder,
            CancellationToken token);
    }

    internal interface IExchangeContextBuilder
    {
        ValueTask<ExchangeContext> Create(Authority authority, bool secure);
    }

    internal class ExchangeContextBuilder : IExchangeContextBuilder
    {
        private readonly ProxyRuntimeSetting _runtimeSetting;

        public ExchangeContextBuilder(ProxyRuntimeSetting runtimeSetting)
        {
            _runtimeSetting = runtimeSetting;
        }

        public ValueTask<ExchangeContext> Create(Authority authority, bool secure)
        {
            var result = new ExchangeContext(authority,
                _runtimeSetting.VariableContext, _runtimeSetting.StartupSetting) {
                Secure = secure
            };

            return  _runtimeSetting.EnforceRules(result, FilterScope.OnAuthorityReceived); 
        }
    }
}
