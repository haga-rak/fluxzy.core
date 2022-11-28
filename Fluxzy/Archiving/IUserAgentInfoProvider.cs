// // Copyright 2022 - Haga Rakotoharivelo
// 

namespace Fluxzy
{
    public interface IUserAgentInfoProvider
    {
        string GetFriendlyName(ulong id, string rawUserAgentValue); 
    }
    

    
    public class DefaultUserAgentInfoProvider : IUserAgentInfoProvider
    {
        public string GetFriendlyName(ulong id, string rawUserAgentValue)
        {
            return rawUserAgentValue;
        }
    }
}