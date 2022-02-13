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
    }
}