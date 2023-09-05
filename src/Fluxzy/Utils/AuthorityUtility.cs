using System;
using System.Linq;

namespace Fluxzy.Utils
{
    public static class AuthorityUtility
    {
        /// <summary>
        /// Parse an authority, accepted separator are ':' and '/'.
        /// </summary>
        /// <param name="rawValue"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool TryParse(string rawValue, out string? host, out int port)
        {
            host = null; 
            port = 0;

            var separators = new[] { ":", "/"};

            foreach (var separator in separators) {
                var lastColumn = rawValue.LastIndexOf(separator, StringComparison.Ordinal);

                if (lastColumn == -1)
                    continue;

                var hostName = rawValue.Substring(0, lastColumn).TrimStart();
                var rawPort = rawValue.Substring(lastColumn + 1).TrimEnd();

                if (!int.TryParse(rawPort, out port))
                    continue;

                host = hostName;

                return true;
            }

            return false;
        }
    }
}