using System;
using System.Linq;

namespace Fluxzy.Utils
{
    public static class AuthorityUtility
    {
        public static bool TryParse(string rawValue, out string?  host, out int port)
        {
            host = null; 
            port = 0;

            var splitted = rawValue.Split(new[] { ":" }, StringSplitOptions.None);
            

            if (splitted.Length < 2)
                return false;

            if (!int.TryParse(splitted[1], out port))
                return false;


            host = string.Join(":", splitted.Take(splitted.Length - 1));

            return true;
        }
    }
}