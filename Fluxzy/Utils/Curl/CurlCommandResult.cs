// Copyright © 2023 Haga RAKOTOHARIVELO

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Utils.Curl
{
    public class CurlCommandResult
    {
        private readonly CurlProxyConfiguration? _configuration;

        public CurlCommandResult(CurlProxyConfiguration? configuration)
        {
            _configuration = configuration;
        }

        public void AddOption(string optionName, string optionValue)
        {
            Args.Add(optionName);
            Args.Add(optionValue);
        }
        
        public void AddArgument(string arg)
        {
            Args.Add(arg);
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        
        public List<string> Args { get; set; } = new();

        public string? PostDataPath { get; set; }

        public string FlatCommandLine
        {
            get
            {
                return string.Join(" ", Args.Select(x => $"\"{x}\""));
            }
        }

        public List<string> ArgsWithProxy
        {
            get
            {
                if (_configuration == null)
                    return Args.ToList();
                
                return Args.Concat(
                               new[] { "-x", $"{_configuration.Host}:{_configuration.Port}" })
                           .ToList();
            }
        }
    }
}
