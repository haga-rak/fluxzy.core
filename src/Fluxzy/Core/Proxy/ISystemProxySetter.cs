// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;

namespace Fluxzy.Core.Proxy
{
    public interface ISystemProxySetter
    {
        Task ApplySetting(SystemProxySetting value);

        Task<SystemProxySetting> ReadSetting();
    }
}
