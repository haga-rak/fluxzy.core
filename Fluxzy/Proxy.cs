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
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public class Proxy : IDisposable, IAsyncDisposable, IExchangeEventSource
    {
        private readonly ProxyStartupSetting _startupSetting;
        private IDownStreamConnectionProvider _downStreamConnectionProvider;
        private CancellationTokenSource _proxyHaltTokenSource = new();

        private SystemProxyRegistration _proxyRegister;

        private bool _disposed;
        private Task _loopTask;
        private bool _started;
        private bool _halted; 
        
        private ProxyOrchestrator _proxyOrchestrator;
        private RealtimeArchiveWriter _writer;

        private  int _currentConcurrentCount = 0; 

        public Proxy(
            ProxyStartupSetting startupSetting,
            ICertificateProvider certificateProvider
            )
        {
            _startupSetting = startupSetting ?? throw new ArgumentNullException(nameof(startupSetting));
            
            _downStreamConnectionProvider =
                new DownStreamConnectionProvider(_startupSetting.BoundPoints);
            
            var throtleStream = startupSetting.GetThrottlerStream();

            Stream ThrottlePolicyStream(string s) => throtleStream;

            var secureConnectionManager = new SecureConnectionUpdater(
               // new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));
                certificateProvider);

            if (_startupSetting.ArchivingPolicy.Type == ArchivingPolicy.ArchivingPolicyType.Directory)
            {
                Directory.CreateDirectory(_startupSetting.ArchivingPolicy.Directory);

                _writer = new DirectoryArchiveWriter(
                    Path.Combine(_startupSetting.ArchivingPolicy.Directory, SessionIdentifier));
            }

            var http1Parser = new Http11Parser(_startupSetting.MaxHeaderLength);
            var poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver()), ITimingProvider.Default, http1Parser,
                _writer);

            ExecutionContext = new ProxyExecutionContext()
            {
                SessionId = SessionIdentifier,
                StartupSetting = startupSetting
            };

            _proxyOrchestrator = new ProxyOrchestrator(new ProxyRuntimeSetting(startupSetting, ExecutionContext, this),
                new ExchangeBuilder(secureConnectionManager, http1Parser), poolBuilder, _writer, this);

            if (!_startupSetting.AlterationRules.Any(t => t.Action is SkipSslTunnelingAction && 
                                                          t.Filter.Children.OfType<AnyFilter>().Any() 
                                                          && t.Filter.Children.Count == 1) 
                && _startupSetting.AutoInstallCertificate)
            {
                CertificateUtility.CheckAndInstallCertificate(startupSetting);
            }

            startupSetting.GetDefaultOutput()
                .WriteLine($@"Listening on {startupSetting.BoundPointsDescription}");
        }

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

            // Proxy is disposing, waiting for all operation to be done 


            //while (_currentConcurrentCount > 0)
            //{
            //    Console.WriteLine("Waiting concurrent");
            //    await Task.Delay(50); 
            //}
        }

        public static Proxy Create(ProxyStartupSetting startupSetting)
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
                        await _proxyOrchestrator.Operate(client, buffer,
                            _proxyHaltTokenSource.Token).ConfigureAwait(false);
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

            if (_startupSetting.RegisterAsSystemProxy)
            {
                var defaultPort = _startupSetting.BoundPoints.OrderByDescending(d => d.Default)
                    .Select(t => t.Port).First(); 

                _proxyRegister = new SystemProxyRegistration(_startupSetting.GetDefaultOutput(), 
                    "127.0.0.1", defaultPort, _startupSetting.ByPassHost.ToArray());
            }
               

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

            _proxyRegister?.Dispose();
            _proxyRegister = null;

            _downStreamConnectionProvider?.Dispose(); // Do not handle new connection to proxy 
            _downStreamConnectionProvider = null;

            _proxyOrchestrator?.Dispose();
            _proxyOrchestrator = null;

            _proxyHaltTokenSource.Cancel();

            _proxyHaltTokenSource?.Dispose();
            _proxyHaltTokenSource = null;
            
            _writer = null;

            _disposed = true;
        }


        public event EventHandler<BeforeRequestEventArgs> BeforeRequest;

        public event EventHandler<BeforeResponseEventArgs> BeforeResponse;

        public event EventHandler<ExchangeCompleteEventArgs> ExchangeComplete;

        public event EventHandler<ConnectionAddedEventArgs> ConnectionAdded;

        public event EventHandler<ConnectionUpdateEventArgs> ConnectionUpdate;
        
        public virtual void OnBeforeRequest(BeforeRequestEventArgs e)
        {
            BeforeRequest?.Invoke(this, e);
        }

        public virtual void OnBeforeResponse(BeforeResponseEventArgs e)
        {
            BeforeResponse?.Invoke(this, e);
        }

        public virtual void OnExchangeComplete(ExchangeCompleteEventArgs e)
        {
            ExchangeComplete?.Invoke(this, e);
        }

        public virtual void OnConnectionAdded(ConnectionAddedEventArgs e)
        {
            ConnectionAdded?.Invoke(this, e);
        }

        public virtual void OnConnectionUpdate(ConnectionUpdateEventArgs e)
        {
            ConnectionUpdate?.Invoke(this, e);
        }
    }

    public class ProxyExecutionContext
    {
        public string SessionId { get; set; }

        public ProxyStartupSetting StartupSetting { get; set; } 
    }


}