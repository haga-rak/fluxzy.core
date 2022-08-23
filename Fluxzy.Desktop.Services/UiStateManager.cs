// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services
{
    public class UiStateManager
    {
        private readonly FluxzySettingManager _settingManager;
        private readonly GlobalFileManager _globalFileManager;
        private readonly ProxyControl _proxyControl;

        public UiStateManager(FluxzySettingManager settingManager,
            GlobalFileManager globalFileManager, ProxyControl proxyControl)
        {
            _settingManager = settingManager;
            _globalFileManager = globalFileManager;
            _proxyControl = proxyControl;
        }


        public async Task<UiState> GetUiState()
        {
            return new UiState()
            {
                FileStateState = _globalFileManager.Current,
                ProxyState = _proxyControl.GetProxyState(),
                SettingsHolder = await _settingManager.Get()
            };
        }
    }
}