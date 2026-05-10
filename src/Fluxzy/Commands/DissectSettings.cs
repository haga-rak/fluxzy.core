// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class DissectSettings : CommandSettings
    {
        [CommandArgument(0, "<input-file-or-directory>")]
        [Description("A fluxzy file or directory to dissect")]
        public string InputFileOrDirectory { get; set; } = null!;

        [CommandOption("-i|--id")]
        [Description("Exchange ids, comma separated exchange list")]
        public string[]? Ids { get; set; }

        [CommandOption("-f|--format")]
        [Description("Specify how to format each matching exchanges to the outputted result. " +
                     "The default value is \"{id} - {url} - {status}\". " +
                     "Use this option to extract specific part of an exchange. Example: \"{response-body}\" for response body content.")]
        [DefaultValue("{id} - {url} - {status}")]
        public string Format { get; set; } = "{id} - {url} - {status}";

        [CommandOption("-o|--output-file")]
        [Description("Output the formatted result to a file instead of stdout")]
        public string? OutputFile { get; set; }

        [CommandOption("-u|--unique")]
        [Description("Result must be unique or exit error")]
        public bool Unique { get; set; }
    }
}
