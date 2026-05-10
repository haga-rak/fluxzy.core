// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Core;

namespace Fluxzy.Cli.Commands
{
    public static class ConsoleHelper
    {
        public static void WriteValidationResult(this IFluxzyConsole console, ValidationResult validationResult)
        {
            var previousColor = Console.ForegroundColor;
            var isRealConsole = console is RealFluxzyConsole;

            if (isRealConsole) {
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
                console.Out.Write(
                    $"[{validationResult.Level} {validationResult.SenderName}] {validationResult.Message}"
                    + Environment.NewLine);
            }
            finally {
                if (isRealConsole) {
                    Console.ForegroundColor = previousColor;
                }
            }
        }
    }
}
