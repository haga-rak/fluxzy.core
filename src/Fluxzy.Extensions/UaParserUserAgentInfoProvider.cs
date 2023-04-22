// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Concurrent;
using UAParser;

namespace Fluxzy.Extensions
{
    /// <summary>
    ///     This user agent provider uses the UAParser library to extract the browser name and version from the user agent
    ///     string.
    /// </summary>
    public class UaParserUserAgentInfoProvider : IUserAgentInfoProvider
    {
        private static readonly Parser Parser = Parser.GetDefault();

        private readonly ConcurrentDictionary<int, string> _friendlyNameCaches = new();

        public string GetFriendlyName(int id, string rawUserAgentValue)
        {
            var userAgentHash = rawUserAgentValue.GetHashCode();

            // A quick non thread safe cache for user agent resolution to prevent overusing of regex 
            // The method should still be "pure"

            if (_friendlyNameCaches.TryGetValue(userAgentHash, out var result))
                return result;

            var clientInfo = Parser.Parse(rawUserAgentValue);

            if (string.IsNullOrWhiteSpace(clientInfo.UA.Major))
                return $"{clientInfo.UA.Family} (#{GetForcedShort(id):X})";

            return _friendlyNameCaches[userAgentHash] =
                $"{clientInfo.UA.Family} {clientInfo.UA.Major} (#{GetForcedShort(id):X})";
        }

        private static short GetForcedShort(int l)
        {
            unchecked {
                return (short) l;
            }
        }
    }
}
