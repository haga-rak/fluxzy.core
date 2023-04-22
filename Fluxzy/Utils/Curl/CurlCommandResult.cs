// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Utils.Curl
{
    public class CurlCommandResult
    {
        private readonly IRunningProxyConfiguration? _configuration;

        public CurlCommandResult(IRunningProxyConfiguration? configuration)
        {
            _configuration = configuration;
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public List<ICommandLineItem> Args { get; set; } = new();

        public string? FileName { get; set; }

        public string FlatCmdArgs => BuildArgs(false, CommandLineVariant.Cmd);

        public string FlatBashArgs => BuildArgs(false, CommandLineVariant.Bash);

        public string FlatCmdArgsWithProxy => BuildArgs(true, CommandLineVariant.Cmd);

        public string FlatBashArgsWithProxy => BuildArgs(true, CommandLineVariant.Bash);

        public void AddOption(string optionName, string optionValue)
        {
            Args.Add(new CommandLineOption(optionName, optionValue));
        }

        public void AddArgument(string arg)
        {
            Args.Add(new CommandLineArgument(arg));
        }

        public string GetProcessCompatibleArgs()
        {
            return FlatCmdArgsWithProxy
                   .Substring("curl ".Length)
                   .Replace(" ^\r\n  ", " ");
        }

        private string BuildArgs(bool withProxy, CommandLineVariant variant)
        {
            var list = new List<ICommandLineItem>();

            list.Add(Args.OfType<CommandLineArgument>().First());

            if (withProxy && _configuration != null) {
                list.Add(new CommandLineOption("-x", $"{_configuration.Host}:{_configuration.Port}"));
                list.Add(new CommandLineOption("--insecure"));
                list.Add(new CommandLineOption("-H", "Accept:"));
                list.Add(new CommandLineOption("-H", "User-Agent:"));
                list.Add(new CommandLineOption("-H", "Content-Type:"));
            }

            list.AddRange(Args.OfType<CommandLineOption>());

            var res = string.Join(variant == CommandLineVariant.Cmd ? " ^\r\n  " : " \\\n  ",
                list.Select(x => x.ToCommandLine(variant)));

            return "curl " + res;
        }
    }

    public class CommandLineArgument : ICommandLineItem
    {
        public CommandLineArgument(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public string ToCommandLine(CommandLineVariant variant)
        {
            if (variant == CommandLineVariant.Cmd)
                return $"\"{Value.Sanitize(CommandLineVariant.Cmd)}\"";

            return $"'{Value.Sanitize(CommandLineVariant.Bash)}'";
        }
    }

    public interface ICommandLineItem
    {
        string ToCommandLine(CommandLineVariant variant);
    }

    public enum CommandLineVariant
    {
        Cmd = 1,
        Bash
    }

    internal static class ProcessArgsSanitizer
    {
        public static string Sanitize(this string args, CommandLineVariant variant)
        {
            if (variant == CommandLineVariant.Cmd)
                return args.Replace("\"", "\"\"");

            return args.Replace("'", "'\\''");
        }
    }
}
