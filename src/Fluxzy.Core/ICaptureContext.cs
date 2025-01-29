// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Threading.Tasks;

namespace Fluxzy
{
    /// <summary>
    ///   Defines the expected behavior of a capture context engine
    /// </summary>
    public interface ICaptureContext : IAsyncDisposable
    {
        /// <summary>
        /// True if available
        /// </summary>
        bool Available { get; }

        /// <summary>
        /// Start the capture engine
        /// </summary>
        /// <returns></returns>
        Task Start();

        /// <summary>
        /// Include a specific socket 
        /// </summary>
        /// <param name="remoteAddress"></param>
        /// <param name="remotePort"></param>
        void Include(IPAddress remoteAddress, int remotePort);

        /// <summary>
        /// Write to a file the content of a specific socket
        /// </summary>
        /// <param name="outFileName"></param>
        /// <param name="remoteAddress"></param>
        /// <param name="remotePort"></param>
        /// <param name="localPort"></param>
        /// <returns>Returns a subscription id</returns>
        long Subscribe(string outFileName, IPAddress remoteAddress, int remotePort, int localPort);

        /// <summary>
        /// Add NSS key to a specific socket
        /// </summary>
        /// <param name="nssKey"></param>
        /// <param name="remoteAddress"></param>
        /// <param name="remotePort"></param>
        /// <param name="localPort"></param>
        void StoreKey(string nssKey, IPAddress remoteAddress, int remotePort, int localPort);

        /// <summary>
        /// clear all
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Flush any in memory data to disk 
        /// </summary>
        void Flush();

        /// <summary>
        /// Unsubscribe a specific socket
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        ValueTask Unsubscribe(long subscription);
    }
}
