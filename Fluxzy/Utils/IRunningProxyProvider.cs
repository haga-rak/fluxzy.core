// Copyright © 2023 Haga RAKOTOHARIVELO

using System.Threading.Tasks;
using Fluxzy.Utils.Curl;

namespace Fluxzy.Utils
{
    public interface IRunningProxyProvider
    {
        Task<IRunningProxyConfiguration> GetConfiguration();
    }
}
