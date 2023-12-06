// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Utils
{
    internal static class EnvironmentUtility
    {
        public static long GetInt64(string environmentVariableName, long defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
                return defaultValue;

            return long.Parse(value); 
        }

        public static int GetInt32(string environmentVariableName, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
                return defaultValue;

            return int.Parse(value); 
        }
    }
}
