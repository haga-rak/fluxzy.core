using Fluxzy.Core.Proxy;

namespace Fluxzy.NativeOp.SystemProxySetup.Win32
{
    internal class WindowsSystemProxySetter : ISystemProxySetter
    {
        public void ApplySetting(SystemProxySetting value)
        {
            WindowsProxyHelper.SetProxySetting(value);
        }

        public SystemProxySetting ReadSetting()
        {
            return WindowsProxyHelper.GetSetting();
        }
    }
}