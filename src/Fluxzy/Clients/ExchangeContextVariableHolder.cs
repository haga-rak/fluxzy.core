// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Clients
{
    public class ExchangeContextVariableHolder
    {
        private readonly Dictionary<string, string> _variables = new();

        public bool TryGet(string key, out string? value)
        {
            return _variables.TryGetValue(key, out value);
        }

        public void Set(string key, string value)
        {
            _variables[key] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FormatVariables(string str)
        {
            return string.Empty;
        }
    }
}
