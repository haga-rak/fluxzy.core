using System;
using System.Linq;

namespace Fluxzy.Utils
{
    public static class SubdomainUtility
    {
        public static bool TryGetSubDomain(string host, out string? subDomain)
        {
            var splittedHost = host.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            if (splittedHost.Length > 2) {
                subDomain = string.Join(".", splittedHost.Skip(1));

                return true;
            }

            subDomain = null;

            return false;
        }
    }
}