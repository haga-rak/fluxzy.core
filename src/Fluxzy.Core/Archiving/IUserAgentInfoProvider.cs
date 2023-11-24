// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy
{
    /// <summary>
    /// Defines a provider for user agent information
    /// </summary>
    public interface IUserAgentInfoProvider
    {
        /// <summary>
        /// Get the friendly name of the user agent
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rawUserAgentValue"></param>
        /// <returns></returns>
        string GetFriendlyName(int id, string rawUserAgentValue);
    }
}
