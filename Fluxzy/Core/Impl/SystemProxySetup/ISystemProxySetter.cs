namespace Fluxzy.Core.SystemProxySetup
{
    internal interface ISystemProxySetter
    {
        void ApplySetting(SystemProxySetting value);

        SystemProxySetting ReadSetting();
    }
}