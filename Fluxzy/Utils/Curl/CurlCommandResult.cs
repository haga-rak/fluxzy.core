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

        public string? FileName { get; set; }

        public string FlatCommandLineArgs
        {
            get
            {
                return
                    string.Join(" ", Args.Select(x =>
                        x.StartsWith("-") && !x.Contains("\"") ?
                            $"{x}" : $"\"{x.Sanitize()}\""));
            }
        }

        public string FlatCommandLineWithProxyArgs
        {
            get
            {
                return 
                    string.Join(" ", ArgsWithProxy.Select(x =>
                        x.StartsWith("-") && !x.Contains("\"") ? 
                            $"{x}": $"\"{x.Sanitize()}\""));
            }
        }

        public List<string> ArgsWithProxy
        {
            get
            {
                if (_configuration == null)
                    return Args.ToList();
                
                return new[] {
                    "-x", $"{_configuration.Host}:{_configuration.Port}", // define proxy
                    "--insecure", // avoid checking certificate
                    "-H", "Accept:", // remove accept 
                    "-H", "User-Agent:", // remove user agent 
                    "-H", "Content-Type:", // remove default content-type
                }.Concat(Args)
                     .ToList();
            }
        }
    }


    internal static class ProcessArgsSanitizer
    {
        public static string Sanitize(this string args)
        {
            return args.Replace("\"", "\"\""); 
        }
    }
}
