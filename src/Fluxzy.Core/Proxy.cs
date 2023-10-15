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
    public class Proxy : IAsyncDisposable
    {
        private readonly IDownStreamConnectionProvider _downStreamConnectionProvider;
        private readonly ICertificateProvider _certificateProvider;
        private readonly CancellationTokenSource _externalCancellationSource;
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
        /// <param name="startupSetting"></param>
        /// <param name="tcpConnectionProvider"></param>
        public Proxy(FluxzySetting startupSetting, ITcpConnectionProvider?  tcpConnectionProvider = null)
			: this (startupSetting, new CertificateProvider(startupSetting, new InMemoryCertificateCache()), 
				new DefaultCertificateAuthorityManager(), tcpConnectionProvider: tcpConnectionProvider)
        {

        }

        public Proxy(
            FluxzySetting startupSetting,
            ICertificateProvider certificateProvider,
            CertificateAuthorityManager certificateAuthorityManager,
            ITcpConnectionProvider? tcpConnectionProvider = null,
            IUserAgentInfoProvider? userAgentProvider = null,
            FromIndexIdProvider? idProvider = null,
            CancellationTokenSource externalCancellationSource = null)
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
                Writer, new DefaultDnsSolver());

            ExecutionContext = new ProxyExecutionContext(SessionIdentifier, startupSetting);

            _runTimeSetting = new ProxyRuntimeSetting(startupSetting, ExecutionContext, tcpConnectionProvider1,
                Writer, IdProvider, userAgentProvider);

            _proxyOrchestrator = new ProxyOrchestrator(_runTimeSetting,
                new FromProxyConnectSourceProvider(secureConnectionManager, IdProvider), poolBuilder);

            if (!StartupSetting.AlterationRules.Any(t => t.Action is SkipSslTunnelingAction &&
                                                         t.Filter is AnyFilter)
                && StartupSetting.AutoInstallCertificate)
                certificateAuthorityManager.CheckAndInstallCertificate(startupSetting.CaCertificate.GetX509Certificate());

            ThreadPoolUtility.AutoAdjustThreadPoolSize(StartupSetting.ConnectionPerHost);

        }

        internal ProxyExecutionContext ExecutionContext { get; }

        internal RealtimeArchiveWriter Writer { get; } = new EventOnlyArchiveWriter();

        internal FromIndexIdProvider IdProvider { get; }

        public FluxzySetting StartupSetting { get; }

        public string SessionIdentifier { get; } = DateTime.Now.ToString("yyyyMMdd-HHmmss");

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
                        if (!_externalCancellationSource.IsCancellationRequested)
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
        ///     Start the proxy and return the end points that the proxy is listening to.
        /// </summary>
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

            return endPoints;
        }

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
