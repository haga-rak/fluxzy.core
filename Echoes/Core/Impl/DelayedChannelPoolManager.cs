using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Echoes.Core
{
    internal class DelayedChannelPoolManager : IServerChannelPoolManager
    {
        private readonly IUpStreamConnectionFactory _upStreamConnectionFactory;
        private readonly IReferenceClock _referenceClock;
        private readonly int _concurrentConnectionCount;
        private readonly int _anticipatedConnectionCount;

        private readonly ConcurrentDictionary<ConnectionStateKey, Lazy<ConnectionState>> _allStates =
            new ConcurrentDictionary<ConnectionStateKey, Lazy<ConnectionState>>();

        private readonly TimeSpan _defaultKeepAliveTimeout = TimeSpan.FromSeconds(5);

        public DelayedChannelPoolManager(IUpStreamConnectionFactory upStreamConnectionFactory,
            IReferenceClock referenceClock,
            int concurrentConnectionCount = 8, int anticipatedConnectionCount = 0)
        {
            _upStreamConnectionFactory = upStreamConnectionFactory;
            _referenceClock = referenceClock;
            _concurrentConnectionCount = concurrentConnectionCount;
            _anticipatedConnectionCount = anticipatedConnectionCount;
        }

        public Task<IUpstreamConnection> CreateTunneledConnection(string hostName, int port)
        {
            return _upStreamConnectionFactory.CreateTunneledConnection(hostName, port); 
        }

        public async Task<IUpstreamConnection> GetRemoteConnection(string hostName, int port, bool secure)
        {
            var key = new ConnectionStateKey(hostName, port, secure);
            var state = _allStates.GetOrAdd(key, (k) => new Lazy<ConnectionState>(() => new ConnectionState(), true)).Value;
            var allowedToCreateNewConnection = false;
            var shouldAnticipateCreation = false;

            do
            {
                try
                {
                    await state.AccessSemaphore.WaitAsync().ConfigureAwait(false);

                    CleanupExpiredConnection(state);

                    // Check for expiry 

                    var currentConnection = state.AvailableConnections.FirstOrDefault();

                    if (currentConnection != null)
                    {
                        state.AvailableConnections.Remove(currentConnection);
                        
                        state.SafeIncrement();

                        // Anticipate creation when no available connection 
                        shouldAnticipateCreation = state.CurrentRunning == 1 && state.AvailableConnections.Count == 0 && !state.AnticipationEngaged;
                        state.AnticipationEngaged = true;

                        return currentConnection;
                    }

                    if (state.CurrentRunning < _concurrentConnectionCount)
                    {
                        // Can create 
                        state.SafeIncrement();
                        allowedToCreateNewConnection = true;
                    }
                }
                finally
                {
                    state.AccessSemaphore.Release();

                    if (shouldAnticipateCreation)
                    {
                        AnticipateConnectionCreation(key, state);
                    }
                }

                if (allowedToCreateNewConnection)
                {
                    try
                    {

                        var res = await _upStreamConnectionFactory.CreateServerConnection(hostName, port, secure, this)
                            .ConfigureAwait(false);


                        return res;
                    }
                    catch
                    {
                        // The connection was not created actually
                        state.SafeDecrement();

                        throw;
                    }
                }

            } while (!await state.WaitNewConnectionSemaphore.WaitAsync(50).ConfigureAwait(false));

            return await GetRemoteConnection(hostName, port, secure).ConfigureAwait(false);
        }

        public Task AnticipateSecureConnectionCreation(string hostName, int port)
        {
            var key = new ConnectionStateKey(hostName, port, true);
            var state = _allStates.GetOrAdd(key, (k) => new Lazy<ConnectionState>(() => new ConnectionState(), true)).Value;
            
            if (state.CurrentRunning < _concurrentConnectionCount)
            {
               
                return _upStreamConnectionFactory
                    .CreateServerConnection(key.Hostname, key.Port, key.Secure, this)
                    .ContinueWith(async t =>
                        {
                            try
                            {
                                await state.AccessSemaphore.WaitAsync().ConfigureAwait(false);
                                state.AvailableConnections.Add(t.Result);
                            }
                            catch (Exception)
                            {
                                // Skip anticipated connection
                            }
                            finally
                            {
                                state.AccessSemaphore.Release();
                                state.SafeIncrement();
                            }

                        }
                    );
            }

            return Task.CompletedTask;
        }

        private void AnticipateConnectionCreation(ConnectionStateKey key, ConnectionState state)
        {
            if (_anticipatedConnectionCount == 0)
                return;

            Task [] creationTask = new Task[_anticipatedConnectionCount];

            for (int i = 0; i < _anticipatedConnectionCount; i++)
            {
                creationTask[i] =
                    _upStreamConnectionFactory
                        .CreateServerConnection(key.Hostname, key.Port, key.Secure, this)
                        .ContinueWith(async t =>
                            {
                                try
                                {
                                    await state.AccessSemaphore.WaitAsync().ConfigureAwait(false);
                                    state.AvailableConnections.Add(t.Result);
                                }
                                catch (Exception)
                                {
                                    // Skip anticipated connection
                                }
                                finally
                                {
                                    state.AccessSemaphore.Release();
                                }

                            }
                            ).Unwrap();
            }


        }

        private void CleanupExpiredConnection(ConnectionState state)
        {
            if (state.AvailableConnections.Count == 0)
                return;

            var now = _referenceClock.Instant();

            foreach (var connection in state.AvailableConnections.ToArray().Where(r => r.ExpireInstant < now))
            {
                state.AvailableConnections.Remove(connection);
                //connection.Dispose();
            }
        }
        
        public async Task Return(IUpstreamConnection upstreamConnection, bool close)
        {
            var key = new ConnectionStateKey(upstreamConnection.Hostname, upstreamConnection.RemotePort,
                upstreamConnection.Secure);

            if (!_allStates.TryGetValue(key, out var connectionState))
                return;

            try
            {
                await connectionState.Value.AccessSemaphore.WaitAsync().ConfigureAwait(false);

                if (close)
                {
                    connectionState.Value.AvailableConnections.Remove(upstreamConnection);
                    upstreamConnection.Dispose();
                    connectionState.Value.SafeDecrement();
                    connectionState.Value.WaitNewConnectionSemaphore.Release();
                }
                else
                {
                    if (connectionState.Value.AvailableConnections.Add(upstreamConnection)) // This handle multiple release 
                    {
                        upstreamConnection.ExpireInstant = _referenceClock.Instant().Add(_defaultKeepAliveTimeout);
                        connectionState.Value.SafeDecrement();
                        connectionState.Value.WaitNewConnectionSemaphore.Release();
                    }
                }
            }
            finally
            {
                connectionState.Value.AccessSemaphore.Release();
            }
        }

        public bool IsNotRevoked(IUpstreamConnection upstreamConnection)
        {
            return true;
        }

        public void Dispose()
        {
            // Fermer tous les upstream connections

            foreach (var connection in
                _allStates.Values
                    .SelectMany(s => s?.Value?.AvailableConnections ?? new HashSet<IUpstreamConnection>())
                    .Where(c => c != null).ToList())
            {
                connection.Dispose(); ;
            }

        }
    }
}