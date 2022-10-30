// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Writers;

namespace Fluxzy.Clients.H11
{
    public class Http11ConnectionPool : IHttpConnectionPool
    {
        private static readonly List<SslApplicationProtocol> Http11Protocols = new() { SslApplicationProtocol.Http11 };
        private readonly RealtimeArchiveWriter _archiveWriter;

        private readonly H1Logger _logger;

        private readonly Queue<Http11ProcessingState> _processingStates = new();
        private readonly ProxyRuntimeSetting _proxyRuntimeSetting;

        private readonly RemoteConnectionBuilder _remoteConnectionBuilder;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly ITimingProvider _timingProvider;

        internal Http11ConnectionPool(
            Authority authority,
            RemoteConnectionBuilder remoteConnectionBuilder,
            ITimingProvider timingProvider,
            ProxyRuntimeSetting proxyRuntimeSetting,
            RealtimeArchiveWriter archiveWriter)
        {
            _remoteConnectionBuilder = remoteConnectionBuilder;
            _timingProvider = timingProvider;
            _proxyRuntimeSetting = proxyRuntimeSetting;
            _archiveWriter = archiveWriter;
            Authority = authority;
            _semaphoreSlim = new SemaphoreSlim(proxyRuntimeSetting.ConcurrentConnection);
            _logger = new H1Logger(authority);

            ITimingProvider.Default.Instant();
        }

        public Authority Authority { get; }

        public bool Complete => false;

        public void Init()
        {
        }

        public ValueTask<bool> CheckAlive()
        {
            return new ValueTask<bool>(true);
        }

        public async ValueTask Send(Exchange exchange, ILocalLink _, RsBuffer buffer,
            CancellationToken cancellationToken)
        {
            ITimingProvider.Default.Instant();

            exchange.HttpVersion = "HTTP/1.1";

            try
            {
                _logger.Trace(exchange, "Begin wait for authority slot");

                if (!_semaphoreSlim.Wait(0))
                    await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                _logger.Trace(exchange.Id, "Acquiring slot");

                lock (_processingStates)
                {
                    var requestDate = _timingProvider.Instant();

                    while (_processingStates.TryDequeue(out var state))
                    {
                        if (requestDate - state.LastUsed >
                            TimeSpan.FromSeconds(_proxyRuntimeSetting.TimeOutSecondsUnusedConnection))
                            // The connection pool exceeds timing connection ..
                            continue;

                        exchange.Connection = state.Connection;
                        _logger.Trace(exchange.Id, () => $"Recycling connection : {exchange.Connection.Id}");

                        break;
                    }
                }

                if (exchange.Connection == null)
                {
                    _logger.Trace(exchange.Id, () => "New connection request");

                    var openingResult =
                        await _remoteConnectionBuilder.OpenConnectionToRemote(
                            exchange.Authority, exchange.Context, Http11Protocols,
                            _proxyRuntimeSetting, cancellationToken);

                    exchange.Connection = openingResult.Connection;

                    openingResult.Connection.HttpVersion = exchange.HttpVersion;

                    if (_archiveWriter != null)
                        _archiveWriter.Update(exchange.Connection, cancellationToken);

                    _logger.Trace(exchange.Id, () => $"New connection obtained: {exchange.Connection.Id}");
                }

                var poolProcessing = new Http11PoolProcessing(_logger);

                try
                {
                    await poolProcessing.Process(exchange, buffer, cancellationToken)
                                        .ConfigureAwait(false);

                    _logger.Trace(exchange.Id, () => "[Process] return");

                    var res = exchange.Complete
                                      .ContinueWith(async completeTask =>
                                      {
                                          if (exchange.Metrics.ResponseBodyEnd == default)
                                              exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();

                                          if (completeTask.Exception != null &&
                                              completeTask.Exception.InnerExceptions.Any())
                                          {
                                              _logger.Trace(exchange.Id,
                                                  () =>
                                                      $"Complete on error {completeTask.Exception.GetType()} : {completeTask.Exception.Message}");

                                              foreach (var exception in completeTask.Exception.InnerExceptions)
                                                  exchange.Errors.Add(new Error("Error while reading response",
                                                      exception));
                                          }
                                          else if (completeTask.IsCompletedSuccessfully && !completeTask.Result)
                                          {
                                              lock (_processingStates)
                                              {
                                                  _processingStates.Enqueue(
                                                      new Http11ProcessingState(exchange.Connection, _timingProvider));
                                              }

                                              _logger.Trace(exchange.Id,
                                                  () => "Complete on success, recycling connection ...");

                                              return;
                                          }
                                          else
                                          {
                                              _logger.Trace(exchange.Id,
                                                  () => "Complete on success, closing connection ...");
                                              // should close connection 
                                          }

                                          await exchange.Connection.ReadStream.DisposeAsync();

                                          if (exchange.Connection.ReadStream != exchange.Connection.WriteStream)
                                              await exchange.Connection.WriteStream.DisposeAsync();
                                      }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Trace(exchange.Id, () => $"Processing error {ex}");

                    throw;
                }
            }
            finally
            {
                _semaphoreSlim.Release();
                ITimingProvider.Default.Instant();
            }
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public void Dispose()
        {
        }
    }

    public class Http11ProcessingState
    {
        public Connection Connection { get; }

        public DateTime LastUsed { get; set; }

        public Http11ProcessingState(Connection connection, ITimingProvider timingProvider)
        {
            Connection = connection;
            LastUsed = timingProvider.Instant();
        }
    }
}
