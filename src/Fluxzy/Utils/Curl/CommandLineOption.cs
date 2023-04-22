// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Utils.Curl
{
    public class CommandLineOption : ICommandLineItem
    {
        public CommandLineOption(string name)
            : this(name, null)
        {
        }

        public CommandLineOption(string name, string? value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string? Value { get; }

        public string ToCommandLine(CommandLineVariant variant)
        {
            if (variant == CommandLineVariant.Cmd)
                return Value == null ? $"{Name}" : $"{Name} \"{Value.Sanitize(CommandLineVariant.Cmd)}\"";

            return Value == null ? $"{Name}" : $"{Name} '{Value.Sanitize(CommandLineVariant.Bash)}'";
        }
    }
}
