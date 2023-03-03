using System.Threading.Tasks;
using Fluxzy.Core.Proxy;

namespace Fluxzy.NativeOps.SystemProxySetup.Win
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