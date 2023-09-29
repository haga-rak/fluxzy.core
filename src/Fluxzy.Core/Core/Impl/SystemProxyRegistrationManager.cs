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

        public Task<SystemProxySetting> Register(IEnumerable<IPEndPoint> endPoints, FluxzySetting fluxzySetting)
        {
            return Register(endPoints.OrderByDescending(t => Equals(t.Address, IPAddress.Loopback)
                                                             || t.Address.Equals(IPAddress.IPv6Loopback)).First(),
                fluxzySetting.ByPassHost.ToArray());
        }

        public async Task<SystemProxySetting> Register(IPEndPoint endPoint, params string[] byPassHosts)
        {
            var existingSetting = await GetSystemProxySetting();

            if (_oldSetting != null && !existingSetting.Equals(_oldSetting))
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

            await _systemProxySetter.ApplySetting(_currentSetting);

            return _currentSetting;
        }

        public async Task<SystemProxySetting> GetSystemProxySetting()
        {
            var existingSetting = _systemProxySetter.ReadSetting();

            return await existingSetting;
        }

        public async Task UnRegister()
        {
            if (_oldSetting != null) {
                await _systemProxySetter.ApplySetting(_oldSetting);
                _oldSetting = null;

                return;
            }

            if (_currentSetting != null) {
                _currentSetting.Enabled = false;
                await _systemProxySetter.ApplySetting(_currentSetting);
                _currentSetting = null;

                return;
            }

            var existingSetting = await GetSystemProxySetting();

            if (existingSetting.Enabled) {
                existingSetting.Enabled = false;
                await _systemProxySetter.ApplySetting(existingSetting);
            }
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
            AppDomain.CurrentDomain.ProcessExit += delegate { UnRegister(); };
        }
    }
}
