// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Proxy
{
    public interface ISystemProxySetter
    {
        void ApplySetting(SystemProxySetting value);

        SystemProxySetting ReadSetting();
    }
}
