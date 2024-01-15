// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Build
{
    internal static class EnvironmentHelper
    {
        /// <summary>
        ///     Obtains GetEvOrFail or throw exception if not found.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public static string GetEvOrFail(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName) ??
                   throw new Exception($"Environment variable \"{variableName}\" must be SET");
        }
    }
}
