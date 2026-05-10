// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class DissectPcapSettings : CommandSettings
    {
        [CommandArgument(0, "<input-file-or-directory>")]
        [Description("Input file or directory")]
        public string InputFileOrDirectory { get; set; } = null!;

        [CommandOption("-o|--output-file")]
        [Description("Input file or directory")]
        public string OutputFile { get; set; } = null!;
    }
}
