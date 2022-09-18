using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Common;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public class Proxy : IDisposable, IAsyncDisposable
    {
        private readonly ITcpConnectionProvider _tcpConnectionProvider;
        private IDownStreamConnectionProvider _downStreamConnectionProvider;
        private CancellationTokenSource _proxyHaltTokenSource = new();

        private bool _disposed;
        private Task _loopTask;
        private bool _started;
        private bool _halted; 
        
        private ProxyOrchestrator _proxyOrchestrator;
        public RealtimeArchiveWriter Writer { get; private set; } = new EventOnlyArchiveWriter();

        private  int _currentConcurrentCount = 0;
        private readonly FromIndexIdProvider _idProvider;
        private readonly string _workingDirectory;

        public Proxy(
            FluxzySetting startupSetting,
            ICertificateProvider certificateProvider, 
            ITcpConnectionProvider tcpConnectionProvider = null
            )
        {
            _tcpConnectionProvider = tcpConnectionProvider ?? ITcpConnectionProvider.Default;
            StartupSetting = startupSetting ?? throw new ArgumentNullException(nameof(startupSetting));
            _idProvider = new FromIndexIdProvider(StartupSetting.ExchangeStartIndex);
            
            _downStreamConnectionProvider =
                new DownStreamConnectionProvider(StartupSetting.BoundPoints);
           

            var secureConnectionManager = new SecureConnectionUpdater(certificateProvider);

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.Directory)
            {
                _workingDirectory = Path.Combine(StartupSetting.ArchivingPolicy.Directory, SessionIdentifier);
                Directory.CreateDirectory(StartupSetting.ArchivingPolicy.Directory);
                Writer = new DirectoryArchiveWriter(_workingDirectory);
            }

            if (StartupSetting.ArchivingPolicy.Type == ArchivingPolicyType.None)
            {
                _tcpConnectionProvider = ITcpConnectionProvider.Default;
            }
            
            var http1Parser = new Http11Parser(StartupSetting.MaxHeaderLength);
            var poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver()), ITimingProvider.Default, http1Parser,
                Writer);

            ExecutionContext = new ProxyExecutionContext()
            {
                SessionId = SessionIdentifier,
                StartupSetting = startupSetting
            };

            var runTimeSetting = new ProxyRuntimeSetting(startupSetting, ExecutionContext, _tcpConnectionProvider, Writer);

            _proxyOrchestrator = new ProxyOrchestrator(runTimeSetting,
                new ExchangeBuilder(secureConnectionManager, http1Parser, _idProvider), poolBuilder);

            if (!StartupSetting.AlterationRules.Any(t => t.Action is SkipSslTunnelingAction && 
                                                          t.Filter.Children.OfType<AnyFilter>().Any() 
                                                          && t.Filter.Children.Count == 1) 
                && StartupSetting.AutoInstallCertificate)
            {
                CertificateUtility.CheckAndInstallCertificate(startupSetting);
            }

            startupSetting.GetDefaultOutput()
                .WriteLine($@"Listening on {startupSetting.BoundPointsDescription}");
        }
        
        public FluxzySetting StartupSetting { get; }

        public ProxyExecutionContext ExecutionContext { get; }

        public string SessionIdentifier { get; } = DateTime.Now.ToString("yyyyMMdd-HHmmss"); 
        
        private async Task MainLoop()
        {

            while (true)
            {
                var client =
                    await _downStreamConnectionProvider.GetNextPendingConnection().ConfigureAwait(false);

                if (client == null)
                    break;

                ProcessingConnection(client);
            }
        }

        public static Proxy Create(FluxzySetting startupSetting)
        {
            return new Proxy(startupSetting, new CertificateProvider(startupSetting, new InMemoryCertificateCache())); 
        }

        private async void ProcessingConnection(TcpClient client)
        {
            Interlocked.Increment(ref _currentConcurrentCount);

            try
            {
                await Task.Yield();

                using (client)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(32 * 1024);

                    try
                    {
                        // already disposed
                        if (_proxyHaltTokenSource == null)
                            return; 

                        await _proxyOrchestrator.Operate(client, buffer, _proxyHaltTokenSource.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        client.Close();
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrentCount);
            }
        }

        /// <summary>
        ///  Start proxy
        /// </summary>
        public void Run()
        {
            if (_disposed)
                throw new InvalidOperationException("This proxy was already disposed");

            if (_started)
                throw new InvalidOperationException("Proxy was already started");

            _started = true;
            
            _downStreamConnectionProvider.Init(_proxyHaltTokenSource.Token);

            _loopTask = Task.Run(MainLoop);
        }
        
        public void Dispose()
        {
            InternalDispose();
        }

        public async ValueTask DisposeAsync()
        {
            InternalDispose();
            
            try
            {
                await _loopTask.ConfigureAwait(false); // Wait for main loop to end
            }
            catch (Exception)
            {
                // Loop task exception 
            }
        }

        private void InternalDispose()
        {
            _halted = true;

            Writer?.Dispose();
            Writer = null;

            _downStreamConnectionProvider?.Dispose(); // Do not handle new connection to proxy 
            _downStreamConnectionProvider = null;

            _proxyOrchestrator?.Dispose();
            _proxyOrchestrator = null;

            _proxyHaltTokenSource.Cancel();

            _proxyHaltTokenSource?.Dispose();
            _proxyHaltTokenSource = null;

            _disposed = true;
        }
    }

    public class ProxyExecutionContext
    {
        public string SessionId { get; set; }

        public FluxzySetting StartupSetting { get; set; } 
    }


}