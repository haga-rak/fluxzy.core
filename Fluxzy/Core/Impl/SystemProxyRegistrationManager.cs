using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Fluxzy.Core.Proxy;

namespace Fluxzy.Core
{
    /// <summary>
    ///     System proxy management is static because related to OS management.
    /// </summary>
    public class SystemProxyRegistrationManager
    {
        private readonly ISystemProxySetter _systemProxySetter;
        private SystemProxySetting? _oldSetting;
        private SystemProxySetting? _currentSetting;
        private bool _registerDone;

        public  SystemProxyRegistrationManager(ISystemProxySetter systemProxySetter)
        {
            _systemProxySetter = systemProxySetter;
        }

        public  SystemProxySetting? Register(IEnumerable<IPEndPoint> endPoints, FluxzySetting fluxzySetting)
        {
            return Register(endPoints.OrderByDescending(t => Equals(t.Address, IPAddress.Loopback)
                                                             || t.Address.Equals(IPAddress.IPv6Loopback)).First(),
                fluxzySetting.ByPassHost.ToArray());
        }

        public SystemProxySetting Register(IPEndPoint endPoint, params string[] byPassHosts)
        {
            var existingSetting = GetSystemProxySetting();

            if (_oldSetting != null && !existingSetting.Equals(_oldSetting))
                _oldSetting = existingSetting;

            if (!_registerDone)
            {
                _registerDone = true;
                ProxyUnregisterOnAppdomainExit();
            }

            var connectableHostName = GetConnectableIpAddr(endPoint.Address);

            _currentSetting = new SystemProxySetting(
                connectableHostName.ToString(),
                endPoint.Port,
                byPassHosts.Concat(new[] { "127.0.0.1" })
                           .ToArray());

            _systemProxySetter.ApplySetting(_currentSetting);

            return _currentSetting;
        }

        public SystemProxySetting GetSystemProxySetting()
        {
            var existingSetting = _systemProxySetter.ReadSetting();

            return existingSetting;
        }

        public void UnRegister()
        {
            if (_oldSetting != null)
            {
                _systemProxySetter.ApplySetting(_oldSetting);
                _oldSetting = null;

                return;
            }

            if (_currentSetting != null)
            {
                _currentSetting.Enabled = false;
                _systemProxySetter.ApplySetting(_currentSetting);
                _currentSetting = null;

                return;
            }

            var existingSetting = GetSystemProxySetting();

            if (existingSetting.Enabled)
            {
                existingSetting.Enabled = false;
                _systemProxySetter.ApplySetting(existingSetting);
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
