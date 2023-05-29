// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy.Clients
{
    internal class ProxyRuntimeSetting
    {
        private readonly FluxzySetting _startupSetting;
        private List<Rule>? _effectiveRules;

        private ProxyRuntimeSetting()
        {
            ArchiveWriter = new EventOnlyArchiveWriter();
            _startupSetting = new FluxzySetting();
            ExecutionContext = null!;
            CertificateValidationCallback = null!;
        }

        public ProxyRuntimeSetting(
            FluxzySetting startupSetting,
            ProxyExecutionContext executionContext,
            ITcpConnectionProvider tcpConnectionProvider,
            RealtimeArchiveWriter archiveWriter,
            IIdProvider idProvider,
            IUserAgentInfoProvider? userAgentProvider)
        {
            ExecutionContext = null!;
            CertificateValidationCallback = null!;
            _startupSetting = startupSetting;
            ExecutionContext = executionContext;
            TcpConnectionProvider = tcpConnectionProvider;
            ArchiveWriter = archiveWriter;
            IdProvider = idProvider;
            UserAgentProvider = userAgentProvider;
            ConcurrentConnection = startupSetting.ConnectionPerHost;
        }

        public static ProxyRuntimeSetting Default { get; } = new() {
            ArchiveWriter = new EventOnlyArchiveWriter()
        };

        public ProxyExecutionContext ExecutionContext { get; }

        public ITcpConnectionProvider TcpConnectionProvider { get; set; } = ITcpConnectionProvider.Default;

        public RealtimeArchiveWriter ArchiveWriter { get; set; }

        /// <summary>
        ///     Process to validate the remote certificate
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        /// <summary>
        /// </summary>
        public int ConcurrentConnection { get; set; } = 8;

        public int TimeOutSecondsUnusedConnection { get; set; } = 4;

        public IIdProvider IdProvider { get; set; } = new FromIndexIdProvider(0, 0);

        public IUserAgentInfoProvider? UserAgentProvider { get; }

        public VariableContext VariableContext { get; } = new();

        public async ValueTask EnforceRules(
            ExchangeContext context, FilterScope filterScope,
            Connection? connection = null, Exchange? exchange = null)
        {
            _effectiveRules ??= _startupSetting.FixedRules()
                                               .Concat(_startupSetting.AlterationRules)
                                               .ToList();

            foreach (var rule in _effectiveRules.Where(a => a.Action.ActionScope == filterScope
                                                            || a.Action.ActionScope == FilterScope.OutOfScope)) {
                await rule.Enforce(
                    context, exchange, connection, filterScope,
                    ExecutionContext.BreakPointManager);
            }

            if (exchange?.RunInLiveEdit ?? false) {
                var breakPointAction = new BreakPointAction();
                var rule = new Rule(breakPointAction, AnyFilter.Default);

                await rule.Enforce(context, exchange, connection, filterScope,
                    ExecutionContext.BreakPointManager);
            }
        }
    }
}
