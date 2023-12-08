// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Fluxzy
{
    /// <summary>
    /// Utility class for creating HttpClient connected to a Fluxzy instance.
    /// </summary>
    public static class HttpClientUtility
    {
        private static IPEndPoint GetValidIpEndPoint(IReadOnlyCollection<IPEndPoint> endPoints)
        {
            var validEndPoint = endPoints.FirstOrDefault();

            if (validEndPoint == null)
                throw new InvalidOperationException("The provided proxy did not have any valid endpoint. not started yet?");

            if (validEndPoint.Address.Equals(IPAddress.Any))
                return new (IPAddress.Loopback, validEndPoint.Port); 
            
            if (validEndPoint.Address.Equals(IPAddress.IPv6Any))
                return new (IPAddress.IPv6Loopback, validEndPoint.Port); 
            
            return validEndPoint;
        }

        /// <summary>
        /// Creates an instance of <see cref="HttpMessageHandler"/> 
        /// </summary>
        /// <param name="endPoints">The collection of IP endpoints to connect to.</param>
        /// <param name="setting">The Fluxzy setting containing the CA certificate.</param>
        /// <returns>An instance of <see cref="HttpMessageHandler"/>.</returns>
        public static HttpMessageHandler CreateHttpMessageHandler(
            IReadOnlyCollection<IPEndPoint> endPoints, FluxzySetting setting)
        {
            var validIpEndPoint = GetValidIpEndPoint(endPoints);
            var thumbPrint = setting.CaCertificate.GetX509Certificate().Thumbprint; 

            var address = validIpEndPoint.Address.AddressFamily  == AddressFamily.InterNetworkV6
                ? $"[{validIpEndPoint.Address}]" : validIpEndPoint.Address.ToString();

            var messageHandler = new HttpClientHandler() {
                Proxy = new WebProxy(address, validIpEndPoint.Port),
                ServerCertificateCustomValidationCallback = (_, _, chain, _) => {
                    if (chain.ChainElements.Count < 1)
                        return false; 
                    
                    var lastChainThumbPrint = 
                        chain.ChainElements[chain.ChainElements.Count - 1].Certificate.Thumbprint;
                    
                    return lastChainThumbPrint == thumbPrint;
                }
            }; 
            
            return messageHandler;
        }

        /// <summary>
        /// Creates an instance of HttpClient with the specified endpoints and settings.
        /// </summary>
        /// <param name="endPoints">The collection of IPEndPoints to use for the HttpClient.</param>
        /// <param name="setting">The FluxzySetting object containing the settings for the HttpClient.</param>
        /// <returns>An instance of HttpClient configured with the provided endpoints and settings.</returns>
        public static HttpClient CreateHttpClient(IReadOnlyCollection<IPEndPoint> endPoints, FluxzySetting setting)
        {
            return new(CreateHttpMessageHandler(endPoints, setting));
        }
    }
}
