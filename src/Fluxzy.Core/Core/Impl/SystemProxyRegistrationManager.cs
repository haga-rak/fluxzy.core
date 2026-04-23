// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Core.Proxy;

namespace Fluxzy.Core
{
    /// <summary>
    ///     System proxy management is static because related to OS management.
    /// </summary>
    public class SystemProxyRegistrationManager
    {
        private readonly ISystemProxySetter _systemProxySetter;
        private SystemProxySetting? _currentSetting;
        private SystemProxySetting? _oldSetting;
        private bool _registerDone;

        public SystemProxyRegistrationManager(ISystemProxySetter systemProxySetter)
        {
            _systemProxySetter = systemProxySetter;
        }

        /// <summary>
        /// Register the system proxy with the given endPoints and bypass hosts
        /// </summary>
        /// <param name="endPoints"></param>
        /// <param name="fluxzySetting"></param>
        /// <returns></returns>
        internal Task<SystemProxySetting> Register(IEnumerable<IPEndPoint> endPoints, FluxzySetting fluxzySetting)
        {
            return Register(endPoints.OrderByDescending(t => Equals(t.Address, IPAddress.Loopback)
                                                             || t.Address.Equals(IPAddress.IPv6Loopback)).First(),
                fluxzySetting.ByPassHost.ToArray());
        }

        /// <summary>
        ///  Register the system proxy with the given endPoint and bypass hosts
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="byPassHosts"></param>
        /// <returns></returns>
        public async Task<SystemProxySetting> Register(IPEndPoint endPoint, params string[] byPassHosts)
        {
            var existingSetting = await GetSystemProxySetting().ConfigureAwait(false);

            if (_oldSetting == null)
                _oldSetting = existingSetting;

            if (!_registerDone) {
                _registerDone = true;
                ProxyUnregisterOnAppdomainExit();
            }

            var connectableHostName = GetConnectableIpAddr(endPoint.Address);

            _currentSetting = new SystemProxySetting(
                connectableHostName.ToString(),
                endPoint.Port,
                byPassHosts);

            await _systemProxySetter.ApplySetting(_currentSetting).ConfigureAwait(false);

            return _currentSetting;
        }

        /// <summary>
        /// Retrieve the current system proxy setting
        /// </summary>
        /// <returns></returns>
        public async Task<SystemProxySetting> GetSystemProxySetting()
        {
            var existingSetting = _systemProxySetter.ReadSetting();

            return await existingSetting;
        }

        /// <summary>
        /// Returns true only if the OS proxy is currently enabled AND bound to the given endpoint.
        /// Unlike <see cref="GetSystemProxySetting"/>, this filters out unrelated proxies
        /// (corporate, Fiddler, another Fluxzy instance on a different port, ...).
        /// </summary>
        public async Task<bool> IsRegisteredOn(IPEndPoint endPoint)
        {
            var setting = await GetSystemProxySetting().ConfigureAwait(false);
            var host = GetConnectableIpAddr(endPoint.Address);

            return setting.MatchesEndPoint(new IPEndPoint(host, endPoint.Port));
        }

        /// <summary>
        /// Overload mirroring the selection logic of <c>Register(IEnumerable&lt;IPEndPoint&gt;, ...)</c>:
        /// prefers the loopback endpoint when multiple are provided.
        /// </summary>
        public Task<bool> IsRegisteredOn(IEnumerable<IPEndPoint> endPoints)
        {
            return IsRegisteredOn(endPoints.OrderByDescending(t => Equals(t.Address, IPAddress.Loopback)
                                                                   || t.Address.Equals(IPAddress.IPv6Loopback))
                                           .First());
        }

        /// <summary>
        /// Unregister any previous setting
        /// </summary>
        /// <returns></returns>
        public async Task UnRegister()
        {
            if (_oldSetting != null) {
                await _systemProxySetter.ApplySetting(_oldSetting).ConfigureAwait(false);
                _oldSetting = null;
                _currentSetting = null;

                return;
            }

            if (_currentSetting != null) {
                _currentSetting.Enabled = false;
                await _systemProxySetter.ApplySetting(_currentSetting).ConfigureAwait(false);
                _currentSetting = null;

                return;
            }

            // No pending state: UnRegister is a no-op. This matters for the ProcessExit
            // handler that fires after an explicit UnRegister — touching the registry
            // here would corrupt the already-restored user setting.
        }
        
        private IPAddress GetConnectableIpAddr(IPAddress address)
        {
            if (address == null)
                return IPAddress.Loopback;

            if (address.Equals(IPAddress.Any))
                return IPAddress.Loopback;

            if (address.Equals(IPAddress.IPv6Any))
                return IPAddress.IPv6Loopback;

            return address;
        }

        private void ProxyUnregisterOnAppdomainExit()
        {
            AppDomain.CurrentDomain.ProcessExit += delegate { _ = UnRegister(); };
        }
    }
}
