using System.Threading.Tasks;

namespace Fluxzy.Core.Proxy
{
    public interface ISystemProxySetter
    {
        void ApplySetting(SystemProxySetting value);

        SystemProxySetting ReadSetting();
    }
}