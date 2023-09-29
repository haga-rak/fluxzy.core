// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fluxzy.Core;

namespace Fluxzy.Rules.Filters
{
    [FilterMetaData(
        LongDescription = "Select exchange according to the exit code of a launched process." +
                          " Evaluation is considered `true` when" +
                          "the process exits with 0 error code.",
        NotSelectable = true
    )]
    public class ExecFilter : Filter
    {
        public ExecFilter(string filename, string arguments)
        {
            Filename = filename;
            Arguments = arguments;
        }

        [FilterDistinctive(Description = "The file to be executed")]
        public string Filename { get; set; }

        [FilterDistinctive(Description = "Command line arguments")]
        public string Arguments { get; set; } = string.Empty;

        [FilterDistinctive(Description = "When this value is set to true, " +
                                         "the request header will written under env var `Exec.RequestHeader` with HTTP/1.1 syntax")]
        public bool WriteHeaderToEnv { get; set; }

        public override string? Description { get; set; } = "ExecFilter: process execution filter";

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        protected override bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, 
            IExchange? exchange, IFilteringContext? filteringContext)
        {
            var fileName = Filename.EvaluateVariable(exchangeContext);

            if (string.IsNullOrWhiteSpace(fileName)) {
                throw new RuleExecutionFailureException($"{nameof(Filename)} cannot be null or empty"); 
            }

            var arguments = Arguments.EvaluateVariable(exchangeContext);

            try {
                var processStartInfo = string.IsNullOrEmpty(arguments)
                    ? new ProcessStartInfo(fileName)
                    : new ProcessStartInfo(fileName, arguments);

                if (WriteHeaderToEnv) {
                    var fullExchangeHeader = (exchange as Exchange)?.Request.Header.GetHttp11Header();

                    if (fullExchangeHeader != null) {
                        processStartInfo.EnvironmentVariables
                                        .Add("Exec.RequestHeader", fullExchangeHeader.ToString());
                    }
                }

                var process = Process.Start(processStartInfo);

                if (process == null)
                    throw new InvalidOperationException("Could not start process");

                // TODO blocking the thread here, should be async
                // Should InternalApply be async ?
                process.WaitForExit(); 

                return process.ExitCode == 0; 
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw new RuleExecutionFailureException($"An error occurs while running process:" +
                                                        $"{nameof(Filename)}", e);
            }
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            yield return new FilterExample(
                           "A filter running `true` process and allowing any exchanges",
                            new ExecFilter("true", "")
                                      );
        }
    }
}
