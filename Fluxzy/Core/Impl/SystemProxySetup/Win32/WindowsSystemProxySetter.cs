namespace Echoes.Core.SystemProxySetup.Win32
{
    internal class WindowsSystemProxySetter : ISystemProxySetter
    {
        public void ApplySetting(ProxySetting value)
        {
            WindowsProxyHelper.SetProxySetting(value);
        }

        public ProxySetting ReadSetting()
        {
            return WindowsProxyHelper.GetSetting();
        }
    }


}