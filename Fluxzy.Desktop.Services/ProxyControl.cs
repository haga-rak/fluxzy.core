using Fluxzy.Core;

namespace Fluxzy.Desktop.Services
{
    public class ProxyControl
    {
        private readonly FluxzySettingManager _settingManager;
        private Proxy?  _proxy; 

        public ProxyControl(FluxzySettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        public async Task Init()
        {
            await UpdateSettings(); 
        }

        private async Task UpdateSettings()
        {
            if (_proxy != null)
            {
                await _proxy.DisposeAsync();
                _proxy = null; 
            }

            var currentSettingHolder = _settingManager.Get();

            _proxy = new Proxy(currentSettingHolder.StartupSetting,
                new CertificateProvider(currentSettingHolder.StartupSetting, new InMemoryCertificateCache()));

            _proxy.Run();
        }

        public Task<bool> SetAsSystemProxy()
        {
            if (_proxy == null)
                return Task.FromResult(false); 

            _proxy.SetAsSystemProxy();

            return Task.FromResult(true); 
        }

        public Task<bool> UnsetAsSystemProxy()
        {
            if (_proxy == null)
                return Task.FromResult(false);

            _proxy.UnsetAsSystemProxy();

            return Task.FromResult(true);
        }

        public ProxyState GetProxyState()
        {
            return new ProxyState()
            {
                IsListening = _proxy != null && _proxy.SystemProxyOn,
                IsSystemProxyOn = _proxy != null,
                BoundConnections = _proxy?.StartupSetting.BoundPoints
                    .Select(b => new ProxyEndPoint(b.Address, b.Port))
                    .ToList() ?? new List<ProxyEndPoint>()
            }; 
        }

    }
}