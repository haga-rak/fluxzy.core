namespace Fluxzy.Core.SystemProxySetup
{
    internal interface ISystemProxySetter
    {
        void ApplySetting(ProxySetting value);

        ProxySetting ReadSetting();
    }
}