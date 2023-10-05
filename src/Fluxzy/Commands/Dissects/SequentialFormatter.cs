// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal class SequentialFormatter
    {
        public async Task Format<TArg>(
            string format,
            Dictionary<string, IDissectionFormatter<TArg>> map,
            StreamWriter stdOutWritter,
            StreamWriter stdErrorWriter, 
            TArg payload)
        {
            var pendingText = new StringBuilder();
            var inExpression = false;

            foreach (var @char in format)
            {
                if (@char == '{')
                {
                    inExpression = true;
                    pendingText.Append(@char);
                    continue;
                }

                if (@char == '}')
                {
                    pendingText.Append(@char);
                    inExpression = false;

                    // Dump expression 

                    var hint = pendingText.ToString(1, pendingText.Length - 2);

                    if (!map.TryGetValue(hint, out var formatter))
                    {
                        await stdOutWritter.WriteAsync(pendingText.ToString());
                        await stdErrorWriter.WriteLineAsync($"WARN: Unknown formatter {hint}");
                    }
                    else
                    {
                        await formatter.Write(payload, stdOutWritter);
                    }

                    pendingText.Clear();

                    continue;
                }

                if (inExpression)
                {
                    pendingText.Append(@char);

                    continue;
                }

                stdOutWritter.Write(@char);
            }

            stdOutWritter.Write(pendingText.ToString());
            stdOutWritter.Flush();
        }
    }
}
