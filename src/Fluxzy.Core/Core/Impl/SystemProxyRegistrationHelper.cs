// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Utils.NativeOps.SystemProxySetup;

namespace Fluxzy.Core
{
    /// <summary>
    /// An helper class to quickly register a system proxy setting
    /// </summary>
    public static class SystemProxyRegistrationHelper
    {
        /// <summary>
        ///  Register as system proxy from the provided endpoint. 
        /// </summary>
        /// <param name="endPoint">Endpoint</param>
        /// <param name="byPassedHosts">List of host bypassing the proxy</param>
        /// <returns>An IAsyncDisposable that restore the proxy settings when disposed</returns>
        public static async Task<IAsyncDisposable> Create(IPEndPoint endPoint, params string[] byPassedHosts)
        {
            var instance =
                new SystemProxyRegistrationManager(new NativeProxySetterManager().Get());

            await instance.Register(endPoint, byPassedHosts);

            return new SystemProxyRegistration(instance);
        }

    }
}
