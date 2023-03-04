// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy
{
    public interface IUserAgentInfoProvider
    {
        string GetFriendlyName(int id, string rawUserAgentValue);
    }

    public class DefaultUserAgentInfoProvider : IUserAgentInfoProvider
    {
        public string GetFriendlyName(int id, string rawUserAgentValue)
        {
            return rawUserAgentValue;
        }
    }
}
