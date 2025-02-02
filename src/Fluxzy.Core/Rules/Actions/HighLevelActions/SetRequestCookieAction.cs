// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    /// Add a cookie to request. If a cookie with the same name already exists, it will be removed.
    /// </summary>
    [ActionMetadata("Add a cookie to request. This action is performed by adding/replacing `Cookie` header in request.")]
    public class SetRequestCookieAction : Action
    {
        public SetRequestCookieAction(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [ActionDistinctive(Description = "Cookie name")]
        public string Name { get; set; }

        [ActionDistinctive(Description = "Cookie value")]
        public string Value { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Set request cookie ({Name}, {Value})";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null)
                return default;

            if (Name == null!)
                throw new RuleExecutionFailureException(
                    $"{nameof(Name)} is mandatory for {nameof(SetRequestCookieAction)}", this);

            var cookieHeaders = exchange.GetRequestHeaders().Where(
                c => c.Name.Span.Equals("cookie", StringComparison.OrdinalIgnoreCase))
                                                  .Select(c => c).ToList();

            // We remove any existing cookie with the same name
            context.RequestHeaderAlterations.Add(new HeaderAlterationDelete("cookie"));

            var actualName = HttpUtility.UrlEncode(Name.EvaluateVariable(context));
            var actualValue = HttpUtility.UrlEncode(Value.EvaluateVariable(context));

            var added = false;

            if (cookieHeaders.Any())
            {

                // Normally we should only have one cookie header, but we never know

                foreach (var cookieHeader in cookieHeaders)
                {
                    if (!cookieHeader.Value.Span.Contains(Name, StringComparison.OrdinalIgnoreCase))
                        continue; // micro optimization to avoid any split allocation

                    // Remove all candidates cookie 
                    var existingCookies = cookieHeader.Value.Span.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(c => c.Trim())
                                             .Where(c => !c.StartsWith($"{actualName}=", StringComparison.Ordinal))
                                             .ToList();

                    existingCookies.Add($"{actualName}={actualValue}");

                    var finalFlatValue = string.Join("; ", existingCookies);

                    context.RequestHeaderAlterations.Add(new HeaderAlterationReplace("cookie", finalFlatValue, true));
                    added = true;
                }
            }

            if (!added)
            {
                // Add if any cookie header was found
                context.RequestHeaderAlterations.Add(new HeaderAlterationAdd("cookie", $"{actualName}={actualValue}"));
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Add request cookie with name `session` and value `123456`",
                new SetRequestCookieAction("session", "123456"));
        }
    }
}
