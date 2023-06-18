// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils
{
    public static class StringHelper
    {
        public static string ToCamelCase(this string pascalCaseString)
        {
            // convert pascalCaseString to camelCaseString

            if (pascalCaseString.Length < 2)
                return pascalCaseString;

            return $"{pascalCaseString.Substring(0, 1).ToLower()}{pascalCaseString.Substring(1)}";
        }

        public static string AddTrailingDotAndUpperCaseFirstChar(this string str)
        {
            if (str.Length < 2)
                return str;

            var partialRes = $"{str.Substring(0, 1).ToUpper()}{str.Substring(1)}".Trim();

            if (partialRes.EndsWith("."))
                return partialRes;

            return $"{partialRes}.";
        }

        public static string EscapeDoubleQuote(this string str)
        {
            return str.Replace("\"", "\\\"");
        }
    }
}
