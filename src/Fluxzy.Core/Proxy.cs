// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Clients;
using Fluxzy.Clients.Dns;
using Fluxzy.Clients.Ssl;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Fluxzy.Clients.Ssl.SChannel;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Traces;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy
{
    /// <summary>
    ///  A proxy capture instance that can be started and disposed.
    /// </summary>
    public class Proxy : IAsyncDisposable
    {
        private readonly IDownStreamConnectionProvider _downStreamConnectionProvider;
        private readonly ICertificateProvider _certificateProvider;
        private readonly CancellationTokenSource? _externalCancellationSource;
        private readonly CancellationTokenSource _proxyHaltTokenSource = new();

        private readonly ProxyOrchestrator _proxyOrchestrator;
        private readonly ProxyRuntimeSetting _runTimeSetting;
        private volatile int _currentConcurrentCount;
        private bool _disposed;
        private bool _halted;
        private Task? _loopTask;
        private bool _started;
        
        /// <summary>
        /// Create a new instance of Proxy with the provided setting.
        /// An InMemoryCertificateCache will be used as the certificate cache.
        /// </summary>
        /// <param name="startupSetting">The startup Setting</param>
        /// <param name="tcpConnectionProvider">The tcp connection provider, if null the default is used</param>
        public Proxy(FluxzySetting startupSetting, ITcpConnectionProvider?  tcpConnectionProvider = null)
			: this (startupSetting, new CertificateProvider(startupSetting.CaCertificate, new InMemoryCertificateCache()), 
				new DefaultCertificateAuthorityManager(), tcpConnectionProvider: tcpConnectionProvider)
        {
        }

        /// <summary>
        ///  Create a new instance with specific providers.
        /// If a provider is not provided the default will be used.
        /// </summary>
        /// <param name="startupSetting">The startup Setting</param>
        /// <param name="certificateProvider">A certificate provider</param>
        /// <param name="certificateAuthorityManager">A certificate authority manager</param>
        /// <param name="tcpConnectionProvider">A tcp connection Provider</param>
        /// <param name="userAgentProvider">An user Agent provider</param>
        /// <param name="idProvider">An id provider</param>
        /// <param name="dnsSolver">Add a custom DNS solver</param>
        /// <param name="externalCancellationSource">An external cancellation token</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Proxy(
            FluxzySetting startupSetting,
            ICertificateProvider certificateProvider,
            CertificateAuthorityManager certificateAuthorityManager,
            ITcpConnectionProvider? tcpConnectionProvider = null,
            IUserAgentInfoProvider? userAgentProvider = null,
            FromIndexIdProvider? idProvider = null,
            IDnsSolver? dnsSolver = null,
            CancellationTokenSource? externalCancellationSource = null)
        {
            _certificateProvider = certificateProvider;
            _externalCancellationSource = externalCancellationSource;
            var tcpConnectionProvider1 = tcpConnectionProvider ?? ITcpConnectionProvider.Default;
            StartupSetting = startupSetting ?? throw new ArgumentNullException(nameof(startupSetting));
            IdProvider = idProvider ?? new FromIndexIdProvider(0, 0);

            _downStreamConnectionProvider =
                new DownStreamConnectionProvider(StartupSetting.BoundPoints);

            var secureConnectionManager = new SecureConnectionUpdater(certificateProvider);

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.Directory
                && StartupSetting.ArchivingPolicy.Directory != null) {
                Directory.CreateDirectory(StartupSetting.ArchivingPolicy.Directory);

                Writer = new DirectoryArchiveWriter(StartupSetting.ArchivingPolicy.Directory,
                    StartupSetting.SaveFilter);
            }

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.None)
                tcpConnectionProvider1 = ITcpConnectionProvider.Default;

            var sslConnectionBuilder = startupSetting.UseBouncyCastle
                ? (ISslConnectionBuilder) new BouncyCastleConnectionBuilder()
                : new SChannelConnectionBuilder();

            var poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, sslConnectionBuilder),
                ITimingProvider.Default,
                Writer, dnsSolver ?? new DefaultDnsSolver());

            ExecutionContext = new ProxyExecutionContext(SessionIdentifier, startupSetting);

            _runTimeSetting = new ProxyRuntimeSetting(startupSetting, ExecutionContext, tcpConnectionProvider1,
                Writer, IdProvider, userAgentProvider);

            _proxyOrchestrator = new ProxyOrchestrator(_runTimeSetting,
                !startupSetting.ReverseMode ? 
                new FromProxyConnectSourceProvider(secureConnectionManager, IdProvider): 
                new ReverseProxyExchangeSourceProvider(certificateProvider, IdProvider, startupSetting.ReverseModeForcedPort), 
                poolBuilder);

            if (!StartupSetting.AlterationRules.Any(t => t.Action is SkipSslTunnelingAction &&
                                                         t.Filter is AnyFilter)
                && StartupSetting.AutoInstallCertificate)
                certificateAuthorityManager.CheckAndInstallCertificate(startupSetting.CaCertificate.GetX509Certificate());

            ThreadPoolUtility.AutoAdjustThreadPoolSize(StartupSetting.ConnectionPerHost);

        }


        internal ProxyExecutionContext ExecutionContext { get; }

        internal RealtimeArchiveWriter Writer { get; } = new EventOnlyArchiveWriter();

        internal FromIndexIdProvider IdProvider { get; }

        /// <summary>
        /// Get the setting that was used to start this proxy. Altering this setting will not affect the proxy.
        /// </summary>
        public FluxzySetting StartupSetting { get; }

        /// <summary>
        ///  Get the unique identifier of this proxy instance.
        /// </summary>
        public string SessionIdentifier { get; } = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        /// <summary>
        ///  Release all resources used by this proxy.
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            InternalDispose();

            try {
                if (_loopTask != null)
                    await _loopTask.ConfigureAwait(false); // Wait for main loop to end

                var n = 100;

                while (_currentConcurrentCount > 0 && n-- > 0) {
                    await Task.Delay(5);
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
                        if (_externalCancellationSource != null && !_externalCancellationSource.IsCancellationRequested)
                            _externalCancellationSource.Cancel();
                    });
            }

            while (true) {
                var client =
                    await _downStreamConnectionProvider.GetNextPendingConnection().ConfigureAwait(false);

                if (client == null)
                    break;

                _ = Task.Run(() => ProcessingConnection(client));
            }
        }

        private async ValueTask ProcessingConnection(TcpClient client)
        {
            var currentCount = Interlocked.Increment(ref _currentConcurrentCount);

            try {
                await Task.Yield();

                using (client) {
                    using var buffer = RsBuffer.Allocate(FluxzySharedSetting.RequestProcessingBuffer);

                    try {
                        // already disposed
                        if (_proxyHaltTokenSource.IsCancellationRequested)
                            return;

                        var closeImmediately = FluxzySharedSetting.OverallMaxConcurrentConnections <
                                              currentCount;

                        await _proxyOrchestrator!.Operate(client, buffer, closeImmediately, _proxyHaltTokenSource.Token)
                                                 .ConfigureAwait(false);
                    }
                    finally {
                        client.Close();
                    }
                }
            }
            catch (Exception ex) {
                // We ignore any parsing errors that may block the proxy
                // TODO : escalate from Serilog To Here

                if (D.EnableTracing)
                {
                    var message = $"Processing error {client.Client.RemoteEndPoint}";
                    D.TraceException(ex, message);
                }
            }
            finally {
                Interlocked.Decrement(ref _currentConcurrentCount);
            }
        }

        /// <summary>
        ///  Start the proxy and return the end points that the proxy is listening to.
        /// </summary>
        /// <returns>Returns an exhaustive list of endpoints that the proxy is listen to</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IReadOnlyCollection<IPEndPoint> Run()
        {
            if (_disposed)
                throw new InvalidOperationException("This proxy was already disposed");

            if (_started)
                throw new InvalidOperationException("Proxy was already started");

            _started = true;

            var endPoints = _downStreamConnectionProvider.Init(_proxyHaltTokenSource!.Token);

            _runTimeSetting.EndPoints = endPoints.ToHashSet();
            _runTimeSetting.ProxyListenPort = endPoints.FirstOrDefault()?.Port ?? 0;

            _loopTask = Task.Run(MainLoop);

            EndPoints = endPoints;
            return endPoints;
        }

        /// <summary>
        /// Gets the collection of IP endpoints associated with this proxy. Returns null if the proxy is not started.
        /// </summary>
        /// <remarks>
        /// The IP endpoints represent the network addresses that the property can be accessed on.
        /// </remarks>
        public IReadOnlyCollection<IPEndPoint>? EndPoints { get; private set; }

        private void InternalDispose()
        {
            if (_halted)
                return;

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
