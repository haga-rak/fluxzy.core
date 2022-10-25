using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Common;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Writers;

namespace Fluxzy
{
    public class Proxy : IAsyncDisposable
    {
        private readonly ITcpConnectionProvider _tcpConnectionProvider;
        private IDownStreamConnectionProvider _downStreamConnectionProvider;
        private CancellationTokenSource? _proxyHaltTokenSource = new();

        private bool _disposed;
        private Task? _loopTask;
        private bool _started;
        private bool _halted;

        private ProxyOrchestrator? _proxyOrchestrator;
        public RealtimeArchiveWriter Writer { get; private set; } = new EventOnlyArchiveWriter();

        private volatile int _currentConcurrentCount;

        public Proxy(
            FluxzySetting startupSetting,
            ICertificateProvider certificateProvider,
            ITcpConnectionProvider tcpConnectionProvider = null
        )
        {
            _tcpConnectionProvider = tcpConnectionProvider ?? ITcpConnectionProvider.Default;
            StartupSetting = startupSetting ?? throw new ArgumentNullException(nameof(startupSetting));
            IdProvider = new FromIndexIdProvider(0, 0);

            _downStreamConnectionProvider =
                new DownStreamConnectionProvider(StartupSetting.BoundPoints);

            var secureConnectionManager = new SecureConnectionUpdater(certificateProvider);

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.Directory
                && StartupSetting.ArchivingPolicy.Directory != null) 
            {
                Directory.CreateDirectory(StartupSetting.ArchivingPolicy.Directory);
                Writer = new DirectoryArchiveWriter(StartupSetting.ArchivingPolicy.Directory);
            }

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.None)
                _tcpConnectionProvider = ITcpConnectionProvider.Default;
            
            var poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver()), ITimingProvider.Default,
                Writer);

            ExecutionContext = new ProxyExecutionContext(SessionIdentifier, startupSetting);

            var runTimeSetting = new ProxyRuntimeSetting(startupSetting, ExecutionContext, _tcpConnectionProvider,
                Writer, IdProvider);

            _proxyOrchestrator = new ProxyOrchestrator(runTimeSetting,
                new ExchangeBuilder(secureConnectionManager,  IdProvider), poolBuilder);

            if (!StartupSetting.AlterationRules.Any(t => t.Action is SkipSslTunnelingAction &&
                                                         t.Filter is AnyFilter)
                && StartupSetting.AutoInstallCertificate)
                CertificateUtility.CheckAndInstallCertificate(startupSetting);
        }

        public IReadOnlyCollection<IPEndPoint> ListenAddresses => _downStreamConnectionProvider.ListenEndpoints;

        internal FromIndexIdProvider IdProvider { get; }

        public FluxzySetting StartupSetting { get; }

        public ProxyExecutionContext ExecutionContext { get; }

        public string SessionIdentifier { get; } = DateTime.Now.ToString("yyyyMMdd-HHmmss");


        public static Proxy Create(FluxzySetting startupSetting)
        {
            return new Proxy(startupSetting, new CertificateProvider(startupSetting, new InMemoryCertificateCache()));
        }

        private async ValueTask MainLoop()
        {
            Writer.Init();

            var taskId = 0;

            while (true) {
                var client =
                    await _downStreamConnectionProvider.GetNextPendingConnection().ConfigureAwait(false);

                if (client == null)
                    break;
                
                ProcessingConnection(client, ++taskId);
            }
        }
        

        private async void ProcessingConnection(TcpClient client, int taskId)
        {
            Interlocked.Increment(ref _currentConcurrentCount);

            try {
                await Task.Yield();

                using (client) {
                    using var buffer = RsBuffer.Allocate(16 * 1024);

                    try {
                        // already disposed
                        if (_proxyHaltTokenSource == null)
                            return;

                        await _proxyOrchestrator!.Operate(client, buffer, _proxyHaltTokenSource.Token)
                                                .ConfigureAwait(false);
                    }
                    finally {
                        client.Close();
                    }
                }
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
        

        public async ValueTask DisposeAsync()
        {
            InternalDispose();

            try {
                await _loopTask!.ConfigureAwait(false); // Wait for main loop to end

                int n = 100; 
                while (_currentConcurrentCount > 0 && n-- > 0)
                {
                    await Task.Delay(5);
                }

            }
            catch (Exception) {
                // Loop task exception 
            }
        }

        private void InternalDispose()
        {
            if (_halted)
                return;

            _halted = true;

            Writer?.Dispose();
            Writer = null;

            _downStreamConnectionProvider?.Dispose(); // Do not handle new connection to proxy 
            _downStreamConnectionProvider = null;

            _proxyOrchestrator?.Dispose();
            _proxyOrchestrator = null;

            _proxyHaltTokenSource?.Cancel();

            _proxyHaltTokenSource?.Dispose();
            _proxyHaltTokenSource = null;

            _disposed = true;
        }
    }

    public class ProxyExecutionContext
    {
        public ProxyExecutionContext(string sessionId, FluxzySetting startupSetting)
        {
            SessionId = sessionId;
            StartupSetting = startupSetting;
        }

        public string SessionId { get; }

        public FluxzySetting StartupSetting { get; }
    }
}