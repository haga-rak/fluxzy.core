// Copyright © 2022 Haga Rakotoharivelo

using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy.Clients
{
    internal class ProxyRuntimeSetting
    {
        private readonly FluxzySetting _startupSetting;

        public static ProxyRuntimeSetting Default { get; } = new();

        private ProxyRuntimeSetting()
        {

        }

        public ProxyRuntimeSetting(
            FluxzySetting startupSetting, 
            ProxyExecutionContext executionContext,
            ITcpConnectionProvider tcpConnectionProvider,
            RealtimeArchiveWriter archiveWriter,
            IIdProvider idProvider)
        {
            _startupSetting = startupSetting;
            ExecutionContext = executionContext;
            TcpConnectionProvider = tcpConnectionProvider;
            ArchiveWriter = archiveWriter;
            IdProvider = idProvider;
            ProxyTlsProtocols = startupSetting.ServerProtocols;
            ConcurrentConnection = startupSetting.ConnectionPerHost;
        }

        public ProxyExecutionContext ExecutionContext { get; }

        public ITcpConnectionProvider TcpConnectionProvider { get; } = ITcpConnectionProvider.Default;


        public RealtimeArchiveWriter ArchiveWriter { get; }

        /// <summary>
        /// Protocols supported by the current proxy 
        /// </summary>
        public SslProtocols ProxyTlsProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls11;


        /// <summary>
        /// Process to validate the remote certificate 
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int ConcurrentConnection { get; set; } = 8;

        public int TimeOutSecondsUnusedConnection { get; set; } = 4;


        public IIdProvider IdProvider { get; set; } = new FromIndexIdProvider(0, 0);

        public async ValueTask EnforceRules(ExchangeContext context, FilterScope filterScope, 
             Connection? connection = null, Exchange? exchange = null)
        {
            //if (exchange == null)
            //    return; 

            foreach (var rule in
                     _startupSetting.AlterationRules.Where(a => a.Action.ActionScope == filterScope))
            {
                await rule.Enforce(context, exchange, connection);
            }
        }
    }

    
}