// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Fluxzy.Cli.Commands
{
    public class PackSettings : CommandSettings
    {
        [CommandArgument(0, "<input-directory>")]
        [Description("a fluxzy folder result to export")]
        public string InputDirectory { get; set; } = null!;

        [CommandArgument(1, "<output-file>")]
        [Description("a fluxzy folder result to export")]
        public string OutputFile { get; set; } = null!;

        [CommandOption("-f|--format")]
        [Description("The output file format among fluxzy, har and saz")]
        public string? Format { get; set; }
    }
}
