// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Fluxzy.Cli.Commands
{
    public static class OptionExtensions
    {
        public static Option<T> Get<T>(this IEnumerable<Option> options, string name)
        {
            return options.OfType<Option<T>>().First(t => t.Name == name);
        }

        public static T Value<T>(this ParseResult parseResult, string name)
        {
            parseResult.

            return parseResult.CommandResult.GetRequiredValue<T>(name);
        }
    }
}
