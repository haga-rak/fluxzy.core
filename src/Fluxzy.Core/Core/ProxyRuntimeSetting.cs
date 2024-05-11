// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy.Core
{
    internal class ProxyRuntimeSetting
    {
        private List<Rule>? _effectiveRules;

        private ProxyRuntimeSetting()
        {
            ArchiveWriter = new EventOnlyArchiveWriter();
            StartupSetting = new FluxzySetting();
            ExecutionContext = null!;
            CertificateValidationCallback = null!;
            ActionMapping = new UserAgentActionMapping(null);
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
            StartupSetting = startupSetting;
            ExecutionContext = executionContext;
            TcpConnectionProvider = tcpConnectionProvider;
            ArchiveWriter = archiveWriter;
            IdProvider = idProvider;
            UserAgentProvider = userAgentProvider;
            ConcurrentConnection = startupSetting.ConnectionPerHost;
            ActionMapping = new UserAgentActionMapping(startupSetting.UserAgentActionConfigurationFile);
        }

        internal static ProxyRuntimeSetting CreateDefault => new() {
            ArchiveWriter = new EventOnlyArchiveWriter()
        };

        public FluxzySetting StartupSetting { get; }

        public ProxyExecutionContext? ExecutionContext { get; }

        public ITcpConnectionProvider TcpConnectionProvider { get; set; } = ITcpConnectionProvider.Default;

        public RealtimeArchiveWriter ArchiveWriter { get; set; }

        public UserAgentActionMapping ActionMapping { get; }

        /// <summary>
        ///     Process to validate the remote certificate
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        /// <summary>
        /// </summary>
        public int ConcurrentConnection { get; set; } = 16;

        public int TimeOutSecondsUnusedConnection { get; set; } = 4;

        public IIdProvider IdProvider { get; set; } = new FromIndexIdProvider(0, 0);

        public IUserAgentInfoProvider? UserAgentProvider { get; }

        public VariableContext VariableContext { get; } = new();

        public HashSet<IPEndPoint> EndPoints { get; set; } = new();

        public int ProxyListenPort { get; set; }

        public void Init()
        {
            var activeRules = StartupSetting.FixedRules()
                                            .Concat(StartupSetting.AlterationRules).ToList();

            var startupContext = new StartupContext(StartupSetting, VariableContext, ArchiveWriter);

            foreach (var rule in activeRules) {
                rule.Action.Init(startupContext);
                rule.Filter.Init(startupContext);
            }

            _effectiveRules ??= activeRules;
        }

        public async ValueTask<ExchangeContext> EnforceRules(
            ExchangeContext context, FilterScope filterScope,
            Connection? connection = null, Exchange? exchange = null)
        {
            foreach (var rule in _effectiveRules!.Where(a =>
                         a.Action.ActionScope == filterScope
                         || a.Action.ActionScope == FilterScope.OutOfScope
                         || (a.Action.ActionScope == FilterScope.CopySibling
                             && a.Action is MultipleScopeAction multipleScopeAction
                             && multipleScopeAction.RunScope == filterScope
                         )
                     )) {
                await rule.Enforce(
                    context, exchange, connection, filterScope,
                    ExecutionContext?.BreakPointManager!).ConfigureAwait(false);
            }

            if (exchange?.RunInLiveEdit ?? false) {
                var breakPointAction = new BreakPointAction();
                var rule = new Rule(breakPointAction, AnyFilter.Default);

                await rule.Enforce(context, exchange, connection, filterScope,
                    ExecutionContext?.BreakPointManager!).ConfigureAwait(false);
            }

            return context;
        }
    }
}
