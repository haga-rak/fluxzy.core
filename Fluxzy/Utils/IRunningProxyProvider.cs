// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Utils.Curl;

namespace Fluxzy.Utils
{
    public interface IRunningProxyProvider
    {
        Task<IRunningProxyConfiguration> GetConfiguration();
    }
}
