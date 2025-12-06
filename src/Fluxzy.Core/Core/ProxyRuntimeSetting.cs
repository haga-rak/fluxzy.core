// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
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
        private volatile IReadOnlyList<Rule>? _effectiveRules;
        private readonly object _ruleUpdateLock = new object();

        private ProxyRuntimeSetting()
        {
            ArchiveWriter = new EventOnlyArchiveWriter();
            StartupSetting = new FluxzySetting();
            ExecutionContext = null!;
            CertificateValidationCallback = null!;
            ActionMapping = new SetUserAgentActionMapping(null);
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
            ActionMapping = new SetUserAgentActionMapping(startupSetting.UserAgentActionConfigurationFile);
        }

        internal static ProxyRuntimeSetting CreateDefault => new() {
            ArchiveWriter = new EventOnlyArchiveWriter()
        };

        public FluxzySetting StartupSetting { get; }

        public ProxyExecutionContext? ExecutionContext { get; }

        public ITcpConnectionProvider TcpConnectionProvider { get; set; } = ITcpConnectionProvider.Default;

        public RealtimeArchiveWriter ArchiveWriter { get; set; }

        public SetUserAgentActionMapping ActionMapping { get; }

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

        public ProxyConfiguration?  GetInternalProxyAuthentication()
        {
            var preferredEndPoint = EndPoints
                .OrderByDescending(r => r.Address.AddressFamily == AddressFamily.InterNetwork)
                .ThenByDescending(r => Equals(r.Address, IPAddress.Any) || Equals(r.Address, IPAddress.IPv6Any))
                .FirstOrDefault();

            if (preferredEndPoint == null)
                return null; 

            var preferredAddress = preferredEndPoint.Address.Equals(IPAddress.Any)
                ? IPAddress.Loopback
                : (preferredEndPoint.Address.Equals(IPAddress.IPv6Any) ? IPAddress.IPv6Loopback
                    : preferredEndPoint.Address);
            
            var validEndPoint = new IPEndPoint(preferredAddress, preferredEndPoint.Port);

            var credentials = StartupSetting.ProxyAuthentication == null ? 
                null : new NetworkCredential(
                    StartupSetting.ProxyAuthentication.Username,
                    StartupSetting.ProxyAuthentication.Password);

            var proxyConfiguration = new ProxyConfiguration(
                validEndPoint.Address.ToString(), validEndPoint.Port, credentials);

            return proxyConfiguration;
        }

        public void Init()
        {
            var activeRules = StartupSetting.AlterationRules
                                            .Concat(StartupSetting.FixedRules())
                                            .ToList();

            var startupContext = new StartupContext(StartupSetting, VariableContext, ArchiveWriter);

            foreach (var rule in activeRules) {
                rule.Action.Init(startupContext);
                rule.Filter.Init(startupContext);
            }

            if (_effectiveRules == null) {
                _effectiveRules = activeRules.AsReadOnly();
            }
        }

        /// <summary>
        /// Updates the active alteration rules at runtime. Thread-safe.
        /// Fixed rules (skip SSL, CA mount, welcome page) are automatically included.
        /// </summary>
        /// <param name="newAlterationRules">New alteration rules to apply</param>
        /// <exception cref="InvalidOperationException">If Init() not called yet</exception>
        /// <exception cref="RuleInitializationException">If rule initialization fails</exception>
        public void UpdateRules(IEnumerable<Rule> newAlterationRules)
        {
            if (_effectiveRules == null) {
                throw new InvalidOperationException("Rules not initialized. Call Init() first.");
            }

            lock (_ruleUpdateLock) {
                try {
                    // Combine new alteration rules with fixed rules
                    var activeRules = newAlterationRules
                                        .Concat(StartupSetting.FixedRules())
                                        .ToList();

                    var startupContext = new StartupContext(StartupSetting, VariableContext, ArchiveWriter);

                    // Initialize all new rules
                    foreach (var rule in activeRules) {
                        rule.Action.Init(startupContext);
                        rule.Filter.Init(startupContext);
                    }

                    // Atomic swap - thread-safe visibility
                    var newReadOnlyList = activeRules.AsReadOnly();
                    System.Threading.Interlocked.Exchange(ref _effectiveRules, newReadOnlyList);
                }
                catch (Exception ex) {
                    throw new RuleInitializationException("Failed to update rules: " + ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Gets current alteration rules (excluding fixed rules).
        /// </summary>
        public IReadOnlyCollection<Rule> GetCurrentAlterationRules()
        {
            var currentRules = _effectiveRules;

            if (currentRules == null) {
                return Array.Empty<Rule>();
            }

            // Identify fixed rules by action type
            var fixedRuleActions = StartupSetting.FixedRules()
                                                 .Select(r => r.Action.GetType())
                                                 .ToHashSet();

            return currentRules
                .Where(r => !fixedRuleActions.Contains(r.Action.GetType()))
                .ToList()
                .AsReadOnly();
        }

        public async ValueTask<ExchangeContext> EnforceRules(
            ExchangeContext context, FilterScope filterScope,
            Connection? connection = null, Exchange? exchange = null)
        {
            try {
                foreach (var rule in _effectiveRules!)
                {
                    if (rule.Action.ActionScope != filterScope &&
                        rule.Action.ActionScope != FilterScope.OutOfScope &&
                        !(rule.Action.ActionScope == FilterScope.CopySibling &&
                          rule.Action is MultipleScopeAction multipleScopeAction &&
                          multipleScopeAction.RunScope == filterScope))
                    {
                        continue;
                    }

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
            }
            catch (Exception e) {
                if (e is RuleExecutionFailureException) {
                    throw;
                }
                
                throw new RuleExecutionFailureException("Error while evaluating rules: " + e.Message, e);
            }

            return context;
        }
    }
}
