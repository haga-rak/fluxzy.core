// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class SequentialFormatter
    {
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async Task Format<TArg>(
            string format,
            Dictionary<string, IDissectionFormatter<TArg>> map,
            StreamWriter stdOutWriter,
            StreamWriter stdErrorWriter, 
            TArg payload)
        {
            var pendingText = new StringBuilder();
            var inExpression = false;
            var escapeWasPrevious = false; 

            foreach (var @char in format)
            {
                if (!escapeWasPrevious && @char == '\\') {
                    escapeWasPrevious = true;
                    continue; 
                }

                if (@char == '{')
                {
                    if (escapeWasPrevious) {
                       // pendingText.Append('\\');
                    }
                    else {
                        if (inExpression)
                        {
                            // we clear buffer for stacked expressions

                            stdOutWriter.Write(pendingText);
                            pendingText.Clear();
                        }

                        inExpression = true;
                        pendingText.Append(@char);
                        continue;
                    }
                }

                if (@char == '}')
                {
                    if (escapeWasPrevious)
                    {
                       // pendingText.Append('\\');
                    }
                    else
                    {
                        if (inExpression)
                        {
                            pendingText.Append(@char);
                            inExpression = false;

                            // Dump expression 

                            var hint = pendingText.ToString(1, pendingText.Length - 2).Trim();

                            if (!map.TryGetValue(hint, out var formatter))
                            {
                                await stdOutWriter.WriteAsync(pendingText.ToString());
                                await stdErrorWriter.WriteLineAsync($"WARN: Unknown formatter {hint}");
                            }
                            else
                            {
                                await formatter.Write(payload, stdOutWriter);
                            }

                            pendingText.Clear();
                            continue;
                        }
                    }
                }

                escapeWasPrevious = false;

                if (inExpression)
                {
                    pendingText.Append(@char);
                    continue;
                }

                stdOutWriter.Write(@char);
            }

            if (escapeWasPrevious) {
                pendingText.Append('\\');
            }

            stdOutWriter.Write(pendingText.ToString());
            stdOutWriter.Flush();
        }
    }
}
