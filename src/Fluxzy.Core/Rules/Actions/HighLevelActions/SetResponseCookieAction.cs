// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    /// Add a response cookie. This action is performed by adding `Set-Cookie` header in response.
    /// </summary>
    [ActionMetadata("Add a response cookie. This action is performed by adding `Set-Cookie` header in response.")]
    public class SetResponseCookieAction : Action
    {
        public SetResponseCookieAction(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [ActionDistinctive(Description = "Cookie name")]
        public string Name { get; set; }

        [ActionDistinctive(Description = "Cookie value")]
        public string Value { get; set; }

        [ActionDistinctive(Description = "Cookie path")]
        public string? Path { get; set; }

        [ActionDistinctive(Description = "Cookie domain")]
        public string? Domain { get; set; } = null;

        [ActionDistinctive(Description = "Cookie expiration date in seconds from now", FriendlyType = "integer")]
        public int? ExpireInSeconds { get; set; } = null;

        [ActionDistinctive(Description = "Cookie max age in seconds", FriendlyType = "integer")]
        public int? MaxAge { get; set; } = null;

        [ActionDistinctive(Description = "HttpOnly property")]
        public bool HttpOnly { get; set; }

        [ActionDistinctive(Description = "Secure property")]
        public bool Secure { get; set; }

        [ActionDistinctive(Description = "Set `SameSite` property. " +
                                         "Usual values are Strict, Lax and None. " +
                                         "[Check](https://developer.mozilla.org/docs/Web/HTTP/Headers/Set-Cookie) ")]
        public string? SameSite { get; set; }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription => $"Set response cookie ({Name}, {Value})";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (Name == null!)
                throw new RuleExecutionFailureException(
                    $"{nameof(Name)} is mandatory for {nameof(SetResponseCookieAction)}");

            if (Value == null!)
                throw new RuleExecutionFailureException(
                    $"{nameof(Value)} is mandatory for {nameof(SetResponseCookieAction)}");

            if (exchange == null)
                return default;

            var actualName = HttpUtility.UrlEncode(Name.EvaluateVariable(context));
            var actualValue = HttpUtility.UrlEncode(Value.EvaluateVariable(context));
            var actualPath = Path.EvaluateVariable(context);

            var cookieBuilder = new System.Text.StringBuilder();

            cookieBuilder.Append($"{actualName}={actualValue}");

            if (!string.IsNullOrWhiteSpace(Domain))
                cookieBuilder.Append($"; Domain={Domain.EvaluateVariable(context)}");

            if (!string.IsNullOrWhiteSpace(Path))
                cookieBuilder.Append($"; Path={actualPath}");

            if (ExpireInSeconds != null || ExpireInSeconds == 0)
            {
                var realExpires = DateTime.Now.AddSeconds(ExpireInSeconds.Value);
                cookieBuilder.Append($"; Expires={realExpires:R}");
            }

            if (MaxAge != null || MaxAge == 0)
                cookieBuilder.Append($"; Max-Age={MaxAge.Value}");

            if (HttpOnly)
                cookieBuilder.Append("; HttpOnly");

            if (Secure)
                cookieBuilder.Append("; Secure");

            if (SameSite != null)
                cookieBuilder.Append($"; SameSite={SameSite}");

            context.ResponseHeaderAlterations.Add(new HeaderAlterationAdd("set-cookie",
                cookieBuilder.ToString()));

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Set a cookie with name `my-cookie` and value `my-value`",
                               new SetResponseCookieAction("my-cookie", "my-value")
                           );

            yield return new ActionExample(
                "Add cookie with all properties ", new SetResponseCookieAction("my-cookie", "my-value")
                {
                    Domain = "example.com",
                    ExpireInSeconds = 3600,
                    HttpOnly = true,
                    MaxAge = 3600,
                    Path = "/",
                    SameSite = "Strict",
                    Secure = true
                });

        }
    }
}
