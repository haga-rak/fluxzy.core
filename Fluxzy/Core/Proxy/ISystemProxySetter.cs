namespace Fluxzy.Core.Proxy
{
    public interface ISystemProxySetter
    {
        void ApplySetting(SystemProxySetting value);

        SystemProxySetting ReadSetting();
    }
}