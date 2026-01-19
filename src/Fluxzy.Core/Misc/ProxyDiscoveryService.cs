// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    /// <summary>
    /// mDNS-based proxy discovery service that broadcasts proxy availability on the LAN.
    /// Allows mobile clients to discover the proxy without manual configuration.
    /// Also responds to mDNS queries from clients looking for the service.
    /// </summary>
    public class ProxyDiscoveryService : IAsyncDisposable
    {
        private readonly MdnsAnnouncerOptions _options;
        private readonly IPAddress _ipAddress;
        private readonly string _txtData;
        private readonly string _instanceName;
        private readonly string _hostFqdn;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private UdpClient? _udpClient;
        private CancellationTokenSource? _listenerCts;
        private Task? _listenerTask;
        private volatile bool _isRunning;
        private bool _disposed;

        /// <summary>
        /// Creates a new proxy discovery service with the specified options.
        /// </summary>
        /// <param name="options">Configuration options for the announcer.</param>
        /// <exception cref="ArgumentException">Thrown when the IP address is invalid.</exception>
        public ProxyDiscoveryService(MdnsAnnouncerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (!IPAddress.TryParse(options.HostIpAddress, out var ipAddress))
                throw new ArgumentException($"Invalid IP address: {options.HostIpAddress}", nameof(options));

            if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Only IPv4 addresses are supported", nameof(options));

            _ipAddress = ipAddress;
            _txtData = BuildTxtData(options);
            _instanceName = $"{options.ServiceName}.{MdnsConstants.ServiceType}";
            _hostFqdn = $"{options.HostName}.{MdnsConstants.Domain}";
        }

        /// <summary>
        /// Gets a value indicating whether the service is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Starts the mDNS announcement service and begins listening for queries.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        public async Task StartAsync(CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_isRunning)
                    return;

                _udpClient = CreateUdpClient();
                JoinMulticastGroup(_udpClient);

                // Send initial announcements
                await SendInitialAnnouncementsAsync(ct).ConfigureAwait(false);

                // Start the listener loop
                _listenerCts = new CancellationTokenSource();
                _listenerTask = ListenForQueriesAsync(_listenerCts.Token);

                _isRunning = true;
            }
            catch (SocketException)
            {
                // Network unavailable or permission denied - clean up and rethrow
                CleanupResources();
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stops the mDNS announcement service and sends a goodbye packet.
        /// </summary>
        public async Task StopAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_isRunning)
                    return;

                _isRunning = false;

                // Stop the listener loop
                if (_listenerCts != null)
                {
                    await _listenerCts.CancelAsync().ConfigureAwait(false);

                    if (_listenerTask != null)
                    {
                        try
                        {
                            await _listenerTask.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when cancelling
                        }
                    }

                    _listenerCts.Dispose();
                    _listenerCts = null;
                    _listenerTask = null;
                }

                // Send goodbye packet
                await SendGoodbyeAsync().ConfigureAwait(false);

                // Leave multicast group and cleanup
                CleanupResources();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Disposes the service, stopping announcements if running.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            await StopAsync().ConfigureAwait(false);
            _lock.Dispose();
        }

        /// <summary>
        /// Builds the announcement packet for testing purposes.
        /// </summary>
        internal byte[] BuildAnnouncementPacket()
        {
            return DnsPacketBuilder.BuildAnnouncementPacket(
                _options.ServiceName,
                _options.HostName,
                _ipAddress,
                (ushort)_options.ProxyPort,
                _txtData);
        }

        /// <summary>
        /// Builds the goodbye packet for testing purposes.
        /// </summary>
        internal byte[] BuildGoodbyePacket()
        {
            return DnsPacketBuilder.BuildGoodbyePacket(
                _options.ServiceName,
                _options.HostName,
                _ipAddress,
                (ushort)_options.ProxyPort,
                _txtData);
        }

        private static string BuildTxtData(MdnsAnnouncerOptions options)
        {
            var metadata = new
            {
                host = options.HostIpAddress,
                port = options.ProxyPort,
                hostName = options.HostName,
                osName = options.OsName,
                fluxzyVersion = options.FluxzyVersion,
                fluxzyStartupSetting = options.FluxzyStartupSetting,
                certEndpoint = options.CertEndpoint
            };

            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private static UdpClient CreateUdpClient()
        {
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsConstants.Port));
            return client;
        }

        private static void JoinMulticastGroup(UdpClient client)
        {
            client.JoinMulticastGroup(MdnsConstants.MulticastAddress);
        }

        private async Task SendInitialAnnouncementsAsync(CancellationToken ct)
        {
            for (var i = 0; i < _options.InitialAnnouncementCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                await SendAnnouncementAsync().ConfigureAwait(false);

                if (i < _options.InitialAnnouncementCount - 1)
                    await Task.Delay(_options.InitialAnnouncementDelayMs, ct).ConfigureAwait(false);
            }
        }

        private async Task SendAnnouncementAsync()
        {
            if (_udpClient == null)
                return;

            try
            {
                var packet = BuildAnnouncementPacket();
                var endpoint = new IPEndPoint(MdnsConstants.MulticastAddress, MdnsConstants.Port);
                await _udpClient.SendAsync(packet, packet.Length, endpoint).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                // Network error - ignore and continue
            }
            catch (ObjectDisposedException)
            {
                // Client was disposed - ignore
            }
        }

        private async Task SendGoodbyeAsync()
        {
            if (_udpClient == null)
                return;

            try
            {
                var packet = BuildGoodbyePacket();
                var endpoint = new IPEndPoint(MdnsConstants.MulticastAddress, MdnsConstants.Port);
                await _udpClient.SendAsync(packet, packet.Length, endpoint).ConfigureAwait(false);
            }
            catch (SocketException)
            {
                // Network error - ignore
            }
            catch (ObjectDisposedException)
            {
                // Client was disposed - ignore
            }
        }

        private async Task ListenForQueriesAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(ct).ConfigureAwait(false);
                    await ProcessReceivedPacketAsync(result.Buffer, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (SocketException)
                {
                    // Network error - continue listening
                }
                catch (ObjectDisposedException)
                {
                    // Client was disposed - stop listening
                    break;
                }
            }
        }

        private async Task ProcessReceivedPacketAsync(byte[] data, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return;

            if (!DnsPacketParser.TryParse(data, out var packetInfo))
                return;

            // Check if this is a query for our service
            if (!DnsPacketParser.IsQueryForService(
                    packetInfo,
                    MdnsConstants.ServiceType,
                    _instanceName,
                    _hostFqdn))
            {
                return;
            }

            // Respond with an announcement
            await SendAnnouncementAsync().ConfigureAwait(false);
        }

        private void CleanupResources()
        {
            if (_udpClient != null)
            {
                try
                {
                    _udpClient.DropMulticastGroup(MdnsConstants.MulticastAddress);
                }
                catch
                {
                    // Ignore errors during cleanup
                }

                _udpClient.Dispose();
                _udpClient = null;
            }
        }
    }
}
