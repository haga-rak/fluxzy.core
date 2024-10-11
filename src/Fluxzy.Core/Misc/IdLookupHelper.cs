// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Misc
{
    public static class IdLookupHelper
    {
        /// <summary>
        /// Parses pattern to a list of integers. `-` is used to define a range. `,` is used to separate values
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static HashSet<int> ParsePattern(string pattern)
        {
            var result = new HashSet<int>();

            var parts = pattern.Split(new[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim());

            foreach (var part in parts)
            {
                if (part.Contains("-"))
                {
                    var rangeParts = part.Split('-');

                    if (rangeParts.Length != 2)
                        continue;

                    if (!int.TryParse(rangeParts[0], out var start))
                        continue;

                    if (!int.TryParse(rangeParts[1], out var end))
                        continue;

                    for (var i = start; i <= end; i++)
                    {
                        result.Add(i);
                    }
                }
                else
                {
                    if (int.TryParse(part, out var value))
                    {
                        result.Add(value);
                    }
                }
            }

            return result;
        }
    }
}
