// // Copyright 2022 - Haga Rakotoharivelo
// 

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