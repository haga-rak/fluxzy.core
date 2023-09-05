// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using System.Web;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Add a response cookie. This action is performed by adding/replacing `Set-Cookie` header in response.
    /// </summary>
    [ActionMetadata("Add a response cookie. This action is performed by adding/replacing `Set-Cookie` header in response.")]
    public class SetResponseCookieAction : Action
    {
        public SetResponseCookieAction()
        {

        }

        [ActionDistinctive(Description = "Cookie name")]
        public string Name { get; set; } = string.Empty;

        [ActionDistinctive(Description = "Cookie value")]
        public string Value { get; set; } = string.Empty;

        [ActionDistinctive(Description = "Cookie path")]
        public string?  Path { get; set; }

        [ActionDistinctive(Description = "Cookie domain")]
        public string?  Domain { get; set; } = string.Empty;

        [ActionDistinctive(Description = "Cookie expiration date in seconds from now`")]
        public int? ExpireInSeconds { get; set; } = null;

        [ActionDistinctive(Description = "Cookie max age in seconds")]
        public int? MaxAge { get; set; } = null;

        [ActionDistinctive(Description = "HttpOnly property")]
        public bool HttpOnly { get; set; }

        [ActionDistinctive(Description = "Secure property")]
        public bool Secure { get; set; }

        [ActionDistinctive(Description = "Set `SameSite` property. " +
                                         "Usual values are Strict, Lax and None. " +
                                         "[Check](https://developer.mozilla.org/docs/Web/HTTP/Headers/Set-Cookie) ")]
        public string ? SameSite { get; set; }

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

            if (Path == null!)
                throw new RuleExecutionFailureException(
                    $"{nameof(Path)} is mandatory for {nameof(SetResponseCookieAction)}");


            if (exchange == null)
                return default;


            var actualName = HttpUtility.UrlEncode(Name.EvaluateVariable(context));
            var actualValue = HttpUtility.UrlEncode(Value.EvaluateVariable(context));
            var actualDomain = HttpUtility.UrlEncode(Value.EvaluateVariable(context));
            var actualPath = Path.EvaluateVariable(context);

            var cookieBuilder = new System.Text.StringBuilder();

            cookieBuilder.Append($"{actualName}={actualValue}");

            if (Domain != null)
                cookieBuilder.Append($"; Domain={actualDomain}");

            if (Path != null)
                cookieBuilder.Append($"; Path={actualPath}");


            if (ExpireInSeconds != null) {
                var realExpires = DateTime.Now.AddSeconds(ExpireInSeconds.Value);
                cookieBuilder.Append($"; Expires={realExpires:R}");
            }

            if (MaxAge != null)
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
    }
}
