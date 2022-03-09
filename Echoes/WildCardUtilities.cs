using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Echoes
{
    internal static class WildCardUtilities
    {
        // If you want to implement "*" only
        public static bool WildCardToRegular(this string pattern, string hostName)
        {
            return Regex.IsMatch(hostName, "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");
        }

        // If you want to implement "*" only
        public static bool WildCardContains(this HashSet<string> list, string hostName)
        {
            if (list.Contains(hostName))
                return true;

            foreach (var pattern in list)
            {
                if (Regex.IsMatch(hostName,
                        "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$"))
                    return true; 
            }

            return false;
        }
    }
}