// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Clients;
using Fluxzy.Clients.Dns;
using Fluxzy.Clients.Ssl;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Clients.Ssl.SChannel;
using Fluxzy.Core;
using Fluxzy.Misc;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Traces;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy
{
    /// <summary>
    ///     A proxy capture instance that can be started and disposed.
    /// </summary>
    public class Proxy : IAsyncDisposable
    {
        private readonly ICertificateProvider _certificateProvider;
        private readonly IDownStreamConnectionProvider _downStreamConnectionProvider;
        private readonly CancellationTokenSource? _externalCancellationSource;
        private readonly CancellationTokenSource _proxyHaltTokenSource = new();

        private readonly ProxyOrchestrator _proxyOrchestrator;
        private readonly ProxyRuntimeSetting _runTimeSetting;
        private volatile int _currentConcurrentCount;
        private bool _disposed;
        private List<ProxyDiscoveryService>? _discoveryServices;
        private bool _halted;
        private Task? _loopTask;
        private bool _started;

        /// <summary>
        ///     Create a new instance of Proxy with the provided setting.
        ///     An InMemoryCertificateCache will be used as the certificate cache.
        /// </summary>
        /// <param name="startupSetting">The startup Setting</param>
        /// <param name="tcpConnectionProvider">The tcp connection provider, if null the default is used</param>
        /// <param name="proxyAuthenticationMethod">Use this authentication method instead of the one provided in FluxzySetting</param>
        public Proxy(
            FluxzySetting startupSetting,
            ITcpConnectionProvider? tcpConnectionProvider = null,
            ProxyAuthenticationMethod? proxyAuthenticationMethod = null)
            : this(startupSetting,
                new CertificateProvider(startupSetting.CaCertificate, new InMemoryCertificateCache()),
                new DefaultCertificateAuthorityManager(), tcpConnectionProvider,
                proxyAuthenticationMethod: proxyAuthenticationMethod)
        {
        }

        /// <summary>
        ///     Create a new instance with specific providers.
        ///     If a provider is not provided the default will be used.
        /// </summary>
        /// <param name="startupSetting">The startup Setting</param>
        /// <param name="certificateProvider">A certificate provider</param>
        /// <param name="certificateAuthorityManager">A certificate authority manager</param>
        /// <param name="tcpConnectionProvider">A tcp connection Provider</param>
        /// <param name="userAgentProvider">An user Agent provider</param>
        /// <param name="idProvider">An id provider</param>
        /// <param name="dnsSolver">Add a custom DNS solver</param>
        /// <param name="externalCancellationSource">An external cancellation token</param>
        /// <param name="proxyAuthenticationMethod">Use this authentication method instead of the one provided in FluxzySetting</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Proxy(
            FluxzySetting startupSetting,
            ICertificateProvider certificateProvider,
            CertificateAuthorityManager certificateAuthorityManager,
            ITcpConnectionProvider? tcpConnectionProvider = null,
            IUserAgentInfoProvider? userAgentProvider = null,
            FromIndexIdProvider? idProvider = null,
            IDnsSolver? dnsSolver = null,
            CancellationTokenSource? externalCancellationSource = null,
            ProxyAuthenticationMethod? proxyAuthenticationMethod = null)
        {
            _certificateProvider = certificateProvider;
            _externalCancellationSource = externalCancellationSource;
            var tcpConnectionProvider1 = tcpConnectionProvider ?? ITcpConnectionProvider.Default;
            StartupSetting = startupSetting ?? throw new ArgumentNullException(nameof(startupSetting));
            IdProvider = idProvider ?? new FromIndexIdProvider(0, 0);

            _downStreamConnectionProvider =
                new DownStreamConnectionProvider(StartupSetting.BoundPoints);

            var secureConnectionManager = new SecureConnectionUpdater(certificateProvider, startupSetting.ServeH2);

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.Directory
                && StartupSetting.ArchivingPolicy.Directory != null) {
                Directory.CreateDirectory(StartupSetting.ArchivingPolicy.Directory);

                Writer = new DirectoryArchiveWriter(StartupSetting.ArchivingPolicy.Directory,
                    StartupSetting.SaveFilter);
            }

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.None) {
                tcpConnectionProvider1 = ITcpConnectionProvider.Default;
            }

            var sslConnectionBuilder = startupSetting.UseBouncyCastle
                ? (ISslConnectionBuilder) new BouncyCastleConnectionBuilder()
                : new SChannelConnectionBuilder();

            var poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, sslConnectionBuilder),
                ITimingProvider.Default,
                Writer, dnsSolver ?? new DefaultDnsResolver());
            
            ExecutionContext = new ProxyExecutionContext(startupSetting);

            _runTimeSetting = new ProxyRuntimeSetting(startupSetting, ExecutionContext, tcpConnectionProvider1,
                Writer, IdProvider, userAgentProvider);

            proxyAuthenticationMethod ??= ProxyAuthenticationMethodBuilder.Create(startupSetting.ProxyAuthentication);

            var exchangeContextBuilder = new ExchangeContextBuilder(_runTimeSetting);

            _proxyOrchestrator = new ProxyOrchestrator(
                _runTimeSetting,
                ExchangeSourceProviderHelper.GetSourceProvider(
                    startupSetting, secureConnectionManager,
                    IdProvider, certificateProvider, proxyAuthenticationMethod, exchangeContextBuilder),
                poolBuilder);

            if (!StartupSetting.AlterationRules.Any(t => t.Action is SkipSslTunnelingAction &&
                                                         t.Filter is AnyFilter)
                && StartupSetting.AutoInstallCertificate) {
                certificateAuthorityManager.CheckAndInstallCertificate(
                    startupSetting.CaCertificate.GetX509Certificate());
            }

            ThreadPoolUtility.AutoAdjustThreadPoolSize(StartupSetting.ConnectionPerHost);
        }

        internal ProxyExecutionContext ExecutionContext { get; }

        internal FromIndexIdProvider IdProvider { get; }

        /// <summary>
        ///     Get the writer that is used by this proxy.
        /// </summary>
        public RealtimeArchiveWriter Writer { get; set;  } = new EventOnlyArchiveWriter();
        
        /// <summary>
        ///     Get the setting that was used to start this proxy. Altering this setting will not affect the proxy.
        /// </summary>
        public FluxzySetting StartupSetting { get; }

        /// <summary>
        ///     Get the unique identifier of this proxy instance.
        /// </summary>
        public string SessionIdentifier { get; } = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        /// <summary>
        ///     Gets the collection of IP endpoints associated with this proxy. Returns null if the proxy is not started.
        /// </summary>
        /// <remarks>
        ///     The IP endpoints represent the network addresses that the property can be accessed on.
        /// </remarks>
        public IReadOnlyCollection<IPEndPoint>? EndPoints { get; private set; }

        /// <summary>
        ///     Release all resources used by this proxy.
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            InternalDispose();

            // Dispose discovery services
            if (_discoveryServices != null)
            {
                foreach (var service in _discoveryServices)
                {
                    try
                    {
                        await service.DisposeAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore disposal errors for discovery services
                    }
                }

                _discoveryServices = null;
            }

            try {
                if (_loopTask != null) {
                    await _loopTask.ConfigureAwait(false); // Wait for main loop to end
                }

                var n = 100;

                while (_currentConcurrentCount > 0 && n-- > 0) {
                    await Task.Delay(5).ConfigureAwait(false);
                }
            }
            catch (Exception) {
                // Loop task exception
            }
        }

        private async ValueTask MainLoop()
        {
            Writer.Init();

            if (StartupSetting.MaxExchangeCount > 0) {
                Writer.RegisterExchangeLimit(StartupSetting.MaxExchangeCount,
                    () => {
                        if (_externalCancellationSource != null &&
                            !_externalCancellationSource.IsCancellationRequested) {
                            _externalCancellationSource.Cancel();
                        }
                    });
            }

            while (true) {
                var client =
                    await _downStreamConnectionProvider.GetNextPendingConnection().ConfigureAwait(false);

                if (client == null) {
                    break;
                }

                _ = Task.Factory.StartNew(() => ProcessingConnection(client), TaskCreationOptions.LongRunning);
            }
        }

        private async void ProcessingConnection(TcpClient client)
        {
            var currentCount = Interlocked.Increment(ref _currentConcurrentCount);

            try {
                await Task.Yield();

                using var _ =  client;

                using var buffer = RsBuffer.Allocate(FluxzySharedSetting.RequestProcessingBuffer);

                try {
                    // already disposed
                    if (_proxyHaltTokenSource.IsCancellationRequested) {
                        return;
                    }

                    var closeImmediately = FluxzySharedSetting.OverallMaxConcurrentConnections <
                                           currentCount;

                    await _proxyOrchestrator!.Operate(client, buffer, closeImmediately, _proxyHaltTokenSource.Token)
                                             .ConfigureAwait(false);
                }
                finally {
                    client.Close();
                }
            }
            catch (Exception ex) {
                // We ignore any parsing errors that may block the proxy
                // TODO : escalate from Serilog To Here

                if (D.EnableTracing) {
                    var message = $"Processing error {client.Client.RemoteEndPoint}";
                    D.TraceException(ex, message);
                }
            }
            finally {
                Interlocked.Decrement(ref _currentConcurrentCount);
            }
        }

        /// <summary>
        ///     Start the proxy and return the end points that the proxy is listening to.
        /// </summary>
        /// <returns>Returns an exhaustive list of endpoints that the proxy is listen to</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IReadOnlyCollection<IPEndPoint> Run()
        {
            if (_disposed) {
                throw new InvalidOperationException("This proxy was already disposed");
            }

            if (_started) {
                throw new InvalidOperationException("Proxy was already started");
            }

            // Init rules 

            _runTimeSetting.Init();

            _started = true;

            var endPoints = _downStreamConnectionProvider.Init(_proxyHaltTokenSource!.Token);

            _runTimeSetting.EndPoints = endPoints.ToHashSet();
            _runTimeSetting.ProxyListenPort = endPoints.FirstOrDefault()?.Port ?? 0;

            _loopTask = Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);

            EndPoints = endPoints;

            if (StartupSetting.EnableDiscoveryService) {
                StartDiscoveryServices(endPoints);
            }

            return endPoints;
        }

        private void StartDiscoveryServices(IReadOnlyCollection<IPEndPoint> endPoints)
        {
            var lanAddresses = endPoints
                .Where(ep => ep.AddressFamily == AddressFamily.InterNetwork)
                .Where(ep => !IPAddress.IsLoopback(ep.Address))
                .Select(ep => (Address: ep.Address, Port: ep.Port))
                .Distinct()
                .ToList();

            if (lanAddresses.Count == 0)
                return;

            lanAddresses = lanAddresses.SelectMany(s => {
                if (s.Address.Equals(IPAddress.Any)) {

                    var allIPv4 = IpUtility.GetAllLocalIps()
                                           .Where(i => i.AddressFamily == AddressFamily.InterNetwork)
                                           .Where(ep => !IPAddress.IsLoopback(ep))
                                           .Select(i => i.MapToIPv4());

                    return allIPv4.Select(ip => (Address: ip, Port: s.Port));
                }


                if (s.Address.Equals(IPAddress.IPv6Any)) {

                    var allIpV6 = IpUtility.GetAllLocalIps()
                                           .Where(ep => !IPAddress.IsLoopback(ep))
                                           .Where(i => i.AddressFamily == AddressFamily.InterNetworkV6);

                    return allIpV6.Select(ip => (Address: ip, Port: s.Port));
                }

                return new[] { s };

            }).ToList();


            var startupSettingString = BuildStartupSettingString();
            var fluxzyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            _discoveryServices = new List<ProxyDiscoveryService>();

            foreach (var (address, port) in lanAddresses)
            {
                var options = new MdnsAnnouncerOptions
                {
                    ServiceName = "Fluxzy",
                    ProxyPort = port,
                    HostIpAddress = address.ToString(),
                    FluxzyVersion = fluxzyVersion,
                    FluxzyStartupSetting = startupSettingString
                };

                try
                {
                    var service = new ProxyDiscoveryService(options);
                    service.StartAsync().GetAwaiter().GetResult();
                    _discoveryServices.Add(service);
                }
                catch
                {
                    // Ignore failures for individual addresses - mDNS is optional
                }
            }
        }

        private string BuildStartupSettingString()
        {
            var parts = new List<string>();

            if (StartupSetting.GlobalSkipSslDecryption)
                parts.Add("NoSSL");

            if (StartupSetting.UseBouncyCastle)
                parts.Add("BC");

            if (StartupSetting.ServeH2)
                parts.Add("H2");

            return parts.Count > 0 ? string.Join(",", parts) : "Default";
        }

        /// <summary>
        /// Updates the alteration rules at runtime without stopping the proxy.
        /// New rules apply to exchanges that begin processing after this call completes.
        /// In-flight exchanges continue with their existing rules.
        /// Fixed rules (SSL skip, CA mount, welcome page) are automatically preserved.
        /// </summary>
        /// <param name="rules">The new set of alteration rules</param>
        /// <exception cref="InvalidOperationException">If proxy not started or disposed</exception>
        /// <exception cref="ArgumentNullException">If rules is null</exception>
        /// <exception cref="RuleInitializationException">If rule initialization fails</exception>
        public void UpdateRules(IEnumerable<Rule> rules)
        {
            if (rules == null) {
                throw new ArgumentNullException(nameof(rules));
            }

            ValidateProxyState();

            _runTimeSetting.UpdateRules(rules);
        }

        /// <summary>
        /// Updates the alteration rules at runtime using a configuration action.
        /// Provides a fluent API for configuring rules similar to FluxzySetting setup.
        /// </summary>
        /// <param name="configureRules">Action to configure rules on a temporary FluxzySetting</param>
        /// <exception cref="InvalidOperationException">If proxy not started or disposed</exception>
        /// <exception cref="ArgumentNullException">If configureRules is null</exception>
        /// <exception cref="RuleInitializationException">If rule initialization fails</exception>
        public void UpdateRules(Action<FluxzySetting> configureRules)
        {
            if (configureRules == null) {
                throw new ArgumentNullException(nameof(configureRules));
            }

            ValidateProxyState();

            // Create temporary setting to collect rules
            var tempSetting = new FluxzySetting();
            configureRules(tempSetting);

            _runTimeSetting.UpdateRules(tempSetting.AlterationRules);
        }

        /// <summary>
        /// Gets a read-only snapshot of currently active alteration rules.
        /// Does not include fixed rules (SSL skip, CA mount, welcome page).
        /// </summary>
        /// <returns>Read-only collection of active alteration rules</returns>
        /// <exception cref="InvalidOperationException">If proxy not started</exception>
        public IReadOnlyCollection<Rule> GetActiveRules()
        {
            ValidateProxyState();

            return _runTimeSetting.GetCurrentAlterationRules();
        }

        private void ValidateProxyState()
        {
            if (_disposed) {
                throw new InvalidOperationException("Proxy has been disposed");
            }

            if (!_started) {
                throw new InvalidOperationException("Proxy has not been started. Call Run() first.");
            }
        }

        private void InternalDispose()
        {
            if (_halted) {
                return;
            }

            _halted = true;

            Writer.Dispose();

            _downStreamConnectionProvider.Dispose(); // Do not handle new connection to proxy 

            _proxyOrchestrator.Dispose();

            _proxyHaltTokenSource.Cancel();

            _proxyHaltTokenSource.Dispose();

            _certificateProvider.Dispose();

            _disposed = true;
        }
    }
}
