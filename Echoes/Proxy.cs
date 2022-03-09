using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Clients;
using Echoes.Clients.Common;
using Echoes.Clients.H2.Encoder.Utils;
using Echoes.Core;

namespace Echoes
{
    public class Proxy : IDisposable, IAsyncDisposable
    {
        private readonly ProxyStartupSetting _startupSetting;
        private IDownStreamConnectionProvider _downStreamConnectionProvider;
        private CancellationTokenSource _proxyHaltTokenSource = new CancellationTokenSource();

        private SystemProxyRegistration _proxyRegister;

        private bool _disposed;
        private Task _loopTask;
        private bool _started;
        private bool _halted; 
        
        private ProxyOrchestrator _proxyOrchestrator;
        private RealtimeArchiveWriter _writer;

        public Proxy(
            ProxyStartupSetting startupSetting,
            ICertificateProvider certificateProvider,
            Func<Exchange, Task> onNewExchange = null,
            ProxyAlterationRule alterationRule = null
            )
        {
            _startupSetting = startupSetting ?? throw new ArgumentNullException(nameof(startupSetting));

            var address = string.IsNullOrWhiteSpace(startupSetting.BoundAddress)
                ? IPAddress.Any
                : IPAddress.Parse(startupSetting.BoundAddress);

            _downStreamConnectionProvider =
                new DownStreamConnectionProvider(address, startupSetting.ListenPort);
            
            var throtleStream = startupSetting.GetThrottlerStream();

            Stream ThrottlePolicyStream(string s) => throtleStream;

            var secureConnectionManager = new SecureConnectionUpdater(
               // new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));
                certificateProvider);

            if (_startupSetting.ArchivingPolicy.Type == ArchivingPolicyType.Directory)
            {
                Directory.CreateDirectory(_startupSetting.ArchivingPolicy.Directory);

                _writer = new DirectoryArchiveWriter(
                    Path.Combine(_startupSetting.ArchivingPolicy.Directory, SessionIdentifier));
            }

            var http1Parser = new Http11Parser(_startupSetting.MaxHeaderLength);
            var poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default, new DefaultDnsSolver()), ITimingProvider.Default, http1Parser,
                _writer);

            _proxyOrchestrator = new ProxyOrchestrator(
                onNewExchange,
                ThrottlePolicyStream, 
                _startupSetting, 
                ClientSetting.Default,
                new ExchangeBuilder(
                    secureConnectionManager, http1Parser), poolBuilder, _writer);

            if (!_startupSetting.SkipSslDecryption && _startupSetting.AutoInstallCertificate)
            {
                CertificateUtility.CheckAndInstallCertificate(startupSetting);
            }

            startupSetting.GetDefaultOutput().WriteLine($@"Listening on {startupSetting.BoundAddress}:{startupSetting.ListenPort}");
        }

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

        private async void ProcessingConnection(TcpClient client)
        {
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
                _proxyRegister = new SystemProxyRegistration(_startupSetting.GetDefaultOutput(), _startupSetting.BoundAddress, _startupSetting.ListenPort, _startupSetting.ByPassHost.ToArray());

            _downStreamConnectionProvider.Init(_proxyHaltTokenSource.Token);
            //_loopTask = Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);

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
    }

}