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
using Fluxzy.Core.Breakpoints;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy
{
    public class Proxy : IAsyncDisposable
    {
        private readonly IDownStreamConnectionProvider _downStreamConnectionProvider;
        private readonly CancellationTokenSource _proxyHaltTokenSource = new();

        private readonly ProxyOrchestrator _proxyOrchestrator;
        private volatile int _currentConcurrentCount;
        private bool _disposed;
        private bool _halted;
        private Task? _loopTask;
        private bool _started;

        public Proxy(
            FluxzySetting startupSetting,
            ICertificateProvider certificateProvider,
            CertificateAuthorityManager certificateAuthorityManager,
            ITcpConnectionProvider? tcpConnectionProvider = null,
            IUserAgentInfoProvider? userAgentProvider = null,
            FromIndexIdProvider? idProvider = null)

        {
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
                new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver(), sslConnectionBuilder),
                ITimingProvider.Default,
                Writer);

            ExecutionContext = new ProxyExecutionContext(SessionIdentifier, startupSetting);

            var runTimeSetting = new ProxyRuntimeSetting(startupSetting, ExecutionContext, tcpConnectionProvider1,
                Writer, IdProvider, userAgentProvider);

            _proxyOrchestrator = new ProxyOrchestrator(runTimeSetting,
                new ExchangeBuilder(secureConnectionManager, IdProvider), poolBuilder);

            if (!StartupSetting.AlterationRules.Any(t => t.Action is SkipSslTunnelingAction &&
                                                         t.Filter is AnyFilter)
                && StartupSetting.AutoInstallCertificate)
                certificateAuthorityManager.CheckAndInstallCertificate(startupSetting);
        }

        public ProxyExecutionContext ExecutionContext { get; }

        public RealtimeArchiveWriter Writer { get; } = new EventOnlyArchiveWriter();

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

            while (true) {
                var client =
                    await _downStreamConnectionProvider.GetNextPendingConnection().ConfigureAwait(false);

                if (client == null)
                    break;

                ProcessingConnection(client);
            }
        }

        private async void ProcessingConnection(TcpClient client)
        {
            Interlocked.Increment(ref _currentConcurrentCount);

            try {
                await Task.Yield();

                using (client) {
                    using var buffer = RsBuffer.Allocate(FluxzySharedSetting.RequestProcessingBuffer);

                    try {
                        // already disposed
                        if (_proxyHaltTokenSource.IsCancellationRequested)
                            return;

                        await _proxyOrchestrator!.Operate(client, buffer, _proxyHaltTokenSource.Token)
                                                 .ConfigureAwait(false);
                    }
                    finally {
                        client.Close();
                    }
                }
            }
            catch {
                // We ignore any parsing errors that may block the proxy
                // TODO : escalate from Serilog To Here
            }
            finally {
                var value = Interlocked.Decrement(ref _currentConcurrentCount);
            }
        }

        /// <summary>
        ///     Start proxy
        /// </summary>
        public IReadOnlyCollection<IPEndPoint> Run()
        {
            if (_disposed)
                throw new InvalidOperationException("This proxy was already disposed");

            if (_started)
                throw new InvalidOperationException("Proxy was already started");

            _started = true;

            var endPoints = _downStreamConnectionProvider.Init(_proxyHaltTokenSource!.Token);

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

            _disposed = true;
        }
    }

    public class ProxyExecutionContext
    {
        public ProxyExecutionContext(string sessionId, FluxzySetting startupSetting)
        {
            SessionId = sessionId;
            StartupSetting = startupSetting;

            BreakPointManager = new BreakPointManager(startupSetting
                                                      .AlterationRules.Where(r => r.Action is BreakPointAction)
                                                      .Select(a => a.Filter));
        }

        public string SessionId { get; }

        public FluxzySetting StartupSetting { get; }

        public BreakPointManager BreakPointManager { get; }
    }
}
