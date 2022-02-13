using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core.Utils;

namespace Echoes.Core
{
    internal class UpStreamConnectionFactory : IUpStreamConnectionFactory
    {
        private readonly ProxyStartupSetting _startupSetting;
        private readonly IDnsSolver _dnsSolver;
        private readonly IReferenceClock _referenceClock;
        private readonly ConcurrentDictionary<string, IPAddress> _dnsCache = new ConcurrentDictionary<string, IPAddress>();

        public UpStreamConnectionFactory(ProxyStartupSetting startupSetting, IDnsSolver dnsSolver, IReferenceClock referenceClock)
        {
            _startupSetting = startupSetting;
            _dnsSolver = dnsSolver;
            _referenceClock = referenceClock;
        }

        public async Task<IUpstreamConnection> CreateTunneledConnection(string hostName, int port)
        {
            var dnsStart = _referenceClock.Instant();

            IPAddress ipAddress;

            try
            {
                ipAddress = _dnsCache.TryGetValue(hostName, out var address)
                    ? address
                    : _dnsCache[hostName] = await _dnsSolver.SolveDns(hostName).ConfigureAwait(false);
            }
            catch (SocketException sex)
            {
                throw new EchoesException($"Host name : {hostName} could not be solved", sex);
            }

            var dnsEnd = _referenceClock.Instant();
            var tcpClient = BuildTcpClient();

            try
            {
                await tcpClient.ConnectAsync(ipAddress, port).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is SocketException sex)
                {
                    if (sex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        throw new EchoesException($"The remote server at {ipAddress} expressly refused connection on {port}.", ex);
                    }

                    if (sex.SocketErrorCode == SocketError.TimedOut)
                    {
                        throw new EchoesException($"The remote server at {ipAddress} {port} did not respond (Time out). ", ex);
                    }

                    throw new EchoesException($"The remote server at {ipAddress} {port} fail to answer. Reason : {sex.SocketErrorCode}.", ex);
                }

                throw;
            }

            var connected = _referenceClock.Instant();

            return new TcpUpstreamConnection(tcpClient, hostName, null, dnsEnd, connected, dnsStart, dnsEnd,
                false, _referenceClock);
        }

        public async Task<IUpstreamConnection> CreateServerConnection(string hostName, int port, bool secure,
            IServerChannelPoolManager poolManager)
        {
            var dnsStart = _referenceClock.Instant();

            IPAddress ipAddress;

            try
            {
                ipAddress = _dnsCache.TryGetValue(hostName, out var address)
                    ? address
                    : _dnsCache[hostName] = await _dnsSolver.SolveDns(hostName).ConfigureAwait(false);
            }
            catch (SocketException sex)
            {
                throw new EchoesException($"Host name : {hostName} could not be solved", sex);
            }
            catch (AggregateException agex)
            {
                if (agex.InnerExceptions.OfType<SocketException>().Any())
                {
                    throw new EchoesException($"Host name : {hostName} could not be solved", agex.InnerExceptions.OfType<SocketException>().First());
                }

                throw;
            }

            var dnsEnd = _referenceClock.Instant();

            var tcpClient = BuildTcpClient();


            try
            {
                await tcpClient.ConnectAsync(ipAddress, port).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is SocketException sex)
                {
                    if (sex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        throw new EchoesException($"The remote server at {ipAddress} expressly refused connection on {port}.", ex);
                    }

                    if (sex.SocketErrorCode == SocketError.TimedOut)
                    {
                        throw new EchoesException($"The remote server at {ipAddress} {port} did not respond (Time out). ", ex);
                    }

                    throw new EchoesException($"The remote server at {ipAddress} {port} fail to answer. Reason : {sex.SocketErrorCode}.", ex);
                }
                
                throw;
            }

            var connected = _referenceClock.Instant();
            //var  connected = referenceDate.AddMilliseconds(Interlocked.Increment(ref counter));
           

            if (secure)
            {
                try
                {
                    var matchCertificate = _startupSetting.ClientCertificateConfiguration?.GetCustomClientCertificate(hostName);

                    // Get matching certificate 

                   // var sslStream = new SslStream(new NetworkStream2(networkStream, tcpClient.Client));
                    var sslStream = new SslStream(
                        tcpClient.GetStream(),
                        true, 
                        (sender, x509Certificate, chain, errors) => true,
                        (sender, host, certificates, remoteCertificate, issuers) =>
                        {
                            if (certificates.Count > 0) {
                                return certificates[0];
                            }

                            return null;
                        });

                    var collection = matchCertificate == null ? new X509CertificateCollection() : 
                        new X509CertificateCollection(new X509Certificate[]{ matchCertificate });
                    
                    await sslStream
                        .AuthenticateAsClientAsync(hostName, collection, _startupSetting.ServerProtocols, false)
                        .ConfigureAwait(false);

                    connected = _referenceClock.Instant();
                    
                    return new TcpUpstreamConnection(tcpClient,
                        hostName, 
                        poolManager, 
                        dnsEnd, 
                        connected, 
                        dnsStart,
                        dnsEnd,
                        true, _referenceClock, sslStream);
                }
                catch (Exception ex)
                {
                    throw new EchoesException($"An error occured when trying to secure a connection to {hostName}:{port}", ex);
                }
            }
            else
            {
                return new TcpUpstreamConnection(tcpClient, hostName, poolManager, dnsEnd, connected, dnsStart, dnsEnd,
                    false, _referenceClock);
            }
        }

        private static TcpClient BuildTcpClient()
        {
            return new TcpClient()
            {
                NoDelay = true,
                SendTimeout = 20 * 1000,
                ReceiveBufferSize = 1024 * 64,
               // ReceiveTimeout = 20,
               // SendBufferSize = 80
            };
        }
    }
}