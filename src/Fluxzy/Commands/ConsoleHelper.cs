// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

namespace Fluxzy.Cli.Commands
{
    public static class ConsoleHelper
    {
        public static void WriteValidationResult(this TextWriter writer, ValidationResult validationResult)
        {
            var consoleColor = Console.ForegroundColor;

            if (writer == Console.Out || writer == Console.Error) {
                if (validationResult.Level == ValidationRuleLevel.Information) {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else if (validationResult.Level == ValidationRuleLevel.Warning) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
            }

            try {
                writer.WriteLine(
                    $@"[{validationResult.Level} {validationResult.SenderName}] {validationResult.Message}");
            }
            finally {
                if (writer == Console.Out || writer == Console.Error) {
                    Console.ForegroundColor = consoleColor;
                }
            }
        }
    }
}
