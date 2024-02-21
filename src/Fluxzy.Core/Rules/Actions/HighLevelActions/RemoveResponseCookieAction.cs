// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///  Remove a response cookie by setting the expiration date to a past date.
    /// </summary>
    [ActionMetadata("Remove a response cookie by setting the expiration date to a past date.")]
    public class RemoveResponseCookieAction: Action
    {
        public RemoveResponseCookieAction(string name)
        {
            Name = name;
        }

        [ActionDistinctive(Description = "Cookie name")]
        public string Name { get; set; }

        public override FilterScope ActionScope { get; } = FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription => $"Remove cookie {Name}"; 

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {   
            var actualName = Name.EvaluateVariable(context);

            var cookieBuilder = new System.Text.StringBuilder();

            cookieBuilder.Append($"{actualName}=");

            var unixDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            cookieBuilder.Append($"; Expires={unixDate:R}");

            context.ResponseHeaderAlterations.Add(new HeaderAlterationAdd("set-cookie",
                cookieBuilder.ToString()));

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Remove a cookie named `JSESSIONID`", 
                new RemoveResponseCookieAction("JSESSIONID"));
        }
    }
}
