using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core;
using Echoes.H2.Encoder.Utils;

namespace Echoes
{
    public class Proxy : IDisposable
    {
        private readonly ProxyStartupSetting _startupSetting;
        private readonly IDownStreamConnectionProvider _downStreamConnectionProvider;
        private readonly CancellationTokenSource _proxyHaltTokenSource = new CancellationTokenSource();

        private SystemProxyRegistration _proxyRegister;
        private bool _disposed;
        private Task _loopTask;
        private bool _started;
        private bool _halted; 

        private long _taskId = 0;
        private readonly ProxyOrchestrator _proxyOrchestrator;
        private readonly Http11Parser _http1Parser;
        private readonly PoolBuilder _poolBuilder;


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

            //_tunneledConnectionManager = 
            //    new TunneledConnectionManager(referenceClock, onNewExchange, ThrottlePolicyStream);

            var secureConnectionManager = new SecureConnectionUpdater(
               // new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));
                certificateProvider);

            _http1Parser = new Http11Parser(_startupSetting.MaxHeaderLength, ArrayPoolMemoryProvider<char>.Default);
            _poolBuilder = new PoolBuilder(
                new RemoteConnectionBuilder(ITimingProvider.Default), ITimingProvider.Default, _http1Parser);

            _proxyOrchestrator = new ProxyOrchestrator(onNewExchange,
                ThrottlePolicyStream, _startupSetting, ClientSetting.Default, new ExchangeBuilder(
                    secureConnectionManager, _http1Parser), _poolBuilder);

            if (!_startupSetting.SkipSslDecryption && _startupSetting.AutoInstallCertificate)
            {
                CertificateUtility.CheckAndInstallCertificate(startupSetting);
            }
            

            startupSetting.GetDefaultOutput().WriteLine($@"Listening on {startupSetting.BoundAddress}:{startupSetting.ListenPort}");
        }

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

            if (_halted)
                throw new InvalidOperationException("This proxy cannot be started again");

            _started = true; 

            if (_startupSetting.RegisterAsSystemProxy)
                _proxyRegister = new SystemProxyRegistration(_startupSetting.GetDefaultOutput(), _startupSetting.BoundAddress, _startupSetting.ListenPort, _startupSetting.ByPassHost.ToArray());

            _downStreamConnectionProvider.Init(_proxyHaltTokenSource.Token);
            //_loopTask = Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning);

            _loopTask = Task.Run(MainLoop);
        }
        
        /// <summary>
        /// Release all resource
        /// </summary>
        /// <returns></returns>
        public async Task Release()
        {
            if (_halted)
                return; 

            _halted = true;
            try
            {
                _proxyRegister?.Dispose(); // Unregister system proxy 
                _proxyOrchestrator.Dispose();
                //_tunneledConnectionManager.Dispose(); // Free all created tunnel 
                _downStreamConnectionProvider.Dispose(); // Do not handle new connection to proxy 
                _proxyHaltTokenSource.Cancel(); // Cancel all pending orcherstrator task 

                await _loopTask.ConfigureAwait(false); // Wait for main loop to end
            }
            catch (Exception)
            {

            }

           // await Task.WhenAll(_runningTasks.Values).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _proxyHaltTokenSource.Dispose();

            _disposed = true; 
        }
    }

}