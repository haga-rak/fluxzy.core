// // Copyright 2022 - Haga Rakotoharivelo
// 

using UAParser;

namespace Fluxzy.Extensions
{
    /// <summary>
    /// This user agent provider uses the UAParser library to extract the browser name and version from the user agent string.
    /// </summary>
    public class UaParserUserAgentInfoProvider : IUserAgentInfoProvider
    {
        private static readonly Parser Parser = Parser.GetDefault(); 
        
        public string GetFriendlyName(ulong id, string rawUserAgentValue)
        {
            var clientInfo = Parser.Parse(rawUserAgentValue);

            if (string.IsNullOrWhiteSpace(clientInfo.UA.Major))
                return $"{clientInfo.UA.Family} (#{GetShortFromLong(id):X})"; 
            
            return $"{clientInfo.UA.Family} {clientInfo.UA.Major} (#{GetShortFromLong(id):X})";
        }

        private static short GetShortFromLong(ulong l)
        {
            unchecked
            {
                return (short)l;
            }
        }
    }
}