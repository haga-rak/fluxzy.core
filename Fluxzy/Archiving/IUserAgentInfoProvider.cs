// // Copyright 2022 - Haga Rakotoharivelo
// 

namespace Fluxzy
{
    public interface IUserAgentInfoProvider
    {
        string GetFriendlyName(string rawUserAgentValue); 
    }
    

    
    public class DefaultUserAgentInfoProvider : IUserAgentInfoProvider
    {
        public string GetFriendlyName(string rawUserAgentValue)
        {
            return rawUserAgentValue;
        }
    }
}