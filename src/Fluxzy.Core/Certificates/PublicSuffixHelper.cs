// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Fluxzy.Certificates
{
    public static class PublicSuffixHelper
    {
        public static HashSet<string> KnownSuffixes { get; }

        static PublicSuffixHelper()
        {
            KnownSuffixes = LoadKnownSuffixes();
        }

        private static HashSet<string> LoadKnownSuffixes()
        {
            using var streamReader = new StreamReader(new MemoryStream(FileStore.PublicSuffixList), Encoding.UTF8);

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (streamReader.ReadLine() is { } line)
            {
                line = line.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                if (line.Contains("*"))
                    continue;

                result.Add(line);
            }

            return result;
        }

        public static string GetRootDomain(string fqdn)
        {
            // returns input if valid IP

            if (IPAddress.TryParse(fqdn, out _))
                return fqdn;

            var publicSuffixes = KnownSuffixes;

            // Split into labels:

            var labels = fqdn.Split('.');
            int n = labels.Length;

            if (n < 2)
                return fqdn;  // no dot at all

            // 1) Find the longest matching public suffix
            //    Start with the last label:

            string suffix = labels[n - 1];
            if (publicSuffixes.Contains(suffix))
            {
                // Try to grow it leftward

                for (int i = n - 2; i >= 0; i--)
                {
                    var candidate = $"{labels[i]}.{suffix}";
                    if (publicSuffixes.Contains(candidate))
                        suffix = candidate;
                    else
                        break;
                }
            }

            // 2) Figure out how many labels the registrable domain has:
            //    (suffix labels) + (the one label immediately preceding it)

            int suffixLabelCount = suffix.Count(ch => ch == '.') + 1;
            int registrableLabelCount = suffixLabelCount + 1;

            // 3) If there are no host labels to the left, return unchanged:

            int hostLabelCount = n - registrableLabelCount;
            if (hostLabelCount <= 0)
                return fqdn;

            // 4) Otherwise drop exactly the first label:
            //    e.g. labels = ["a","b","c","d","e","fluxzy","io"]
            //          → join from index=1 → ["b","c","d","e","fluxzy","io"]

            return string.Join(".", labels, 1, n - 1);
        }

    }

    public class SolveDomainResult
    {
        public SolveDomainResult(string main, string wildCard)
        {
            Main = main;
            WildCard = wildCard;
        }

        public string Main { get; }

        public string WildCard { get;  }
    }
}
