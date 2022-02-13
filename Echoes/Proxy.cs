﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Echoes.Core;

namespace Echoes
{
    public class Proxy : IDisposable
    {
        private readonly ProxyStartupSetting _startupSetting;
        private readonly IDownStreamConnectionProvider _downStreamConnectionProvider;
        private readonly IServerChannelPoolManager _poolManager;
        private readonly TunneledConnectionManager _tunneledConnectionManager;
        private readonly CancellationTokenSource _proxyHaltTokenSource = new CancellationTokenSource();

        private SystemProxyRegistration _proxyRegister;
        private bool _disposed;
        private Task _loopTask;
        private bool _started;
        private bool _halted; 

        private long _taskId = 0;
        private readonly IDictionary<long, Task> _runningTasks = new ConcurrentDictionary<long, Task>(); // Dictionary<long, Task>();
        private readonly ProxyOrchestrator _proxyOrchestrator;


        public Proxy(
            ProxyStartupSetting startupSetting,
            ICertificateProvider certificateProvider,
            Func<HttpExchange, Task> onNewExchange = null,
            ProxyAlterationRule alerteAlterationRule = null
            )
        {
            _startupSetting = startupSetting ?? throw new ArgumentNullException(nameof(startupSetting));

            var address = string.IsNullOrWhiteSpace(startupSetting.BoundAddress)
                ? IPAddress.Any
                : IPAddress.Parse(startupSetting.BoundAddress);

            var referenceClock = new ReferenceClock();

            _downStreamConnectionProvider =
                new DownStreamConnectionProvider(address, startupSetting.ListenPort, referenceClock);

            _poolManager = new DelayedChannelPoolManager(
                new UpStreamConnectionFactory(_startupSetting, new DefaultDnsSolver(), referenceClock), referenceClock,
                startupSetting.ConnectionPerHost, startupSetting.AnticipatedConnectionPerHost);

            var throtleStream = startupSetting.GetThrottlerStream();

            Stream ThrottlePolicyStream(string s) => throtleStream;

            _tunneledConnectionManager = new TunneledConnectionManager(referenceClock, onNewExchange, ThrottlePolicyStream);


            var secureConnectionManager = new SecureConnectionUpdater(
               // new CertificateProvider(startupSetting, new FileSystemCertificateCache(startupSetting)));
                certificateProvider);

            var proxyMessageReader = new ProxyMessageReader(startupSetting, secureConnectionManager, _poolManager, referenceClock);

            _proxyOrchestrator = new ProxyOrchestrator(
                proxyMessageReader, 
                new UpStreamClientFactory(_poolManager, _tunneledConnectionManager, alerteAlterationRule, referenceClock),
                onNewExchange, 
                ThrottlePolicyStream);

            if (!_startupSetting.SkipSslDecryption && _startupSetting.AutoInstallCertificate)
            {
                CertificateUtility.CheckAndInstallCertificate(startupSetting);
            }
            

            startupSetting.GetDefaultOutput().WriteLine($@"Listening on {startupSetting.BoundAddress}:{startupSetting.ListenPort}");
        }

        private async Task MainLoop()
        {
            using (var dispatcher = new ProxyConnectionPool<ProxyPoolTask>(8, ProcessingConnection))
            {

                await dispatcher.WaitForInit().ConfigureAwait(false);

                while (true)
                {
                    // Blocking connection to provider
                    IDownStreamConnection connection =
                        await _downStreamConnectionProvider.GetNextPendingConnection().ConfigureAwait(false);

                    if (connection == null)
                        break;

                    dispatcher.PostWork(new ProxyPoolTask(connection, Interlocked.Increment(ref _taskId), _proxyHaltTokenSource.Token));
                }
            }
        }

        private async void ProcessingConnection(ProxyPoolTask proxyPoolTask)
        {
            // var currentTask =
            await _proxyOrchestrator.Operate(proxyPoolTask.Connection, _proxyHaltTokenSource.Token).ConfigureAwait(false);
            
                

            //try
            //{
            //   // _runningTasks[proxyTask.TaskId] = currentTask;
            //   // OnProcessingTaskDone(proxyTask.TaskId, currentTask);
            //}
            //catch (Exception ex)
            //{

            //}
        }

        private void OnProcessingTaskDone(long taskId, Task currentTask)
        {
            currentTask.ContinueWith(t =>
            {
                try
                {
                    return _runningTasks.Remove(taskId);
                }
                catch (Exception)
                {
                    return false; 
                }
            });
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
                _tunneledConnectionManager.Dispose(); // Free all created tunnel 
                _downStreamConnectionProvider.Dispose(); // Do not handle new connection to proxy 
                _poolManager.Dispose(); // Free all connection pool currently maintained
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