// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata(
        "Forward request to a specific URL. This action makes fluxzy act as a reverse proxy. " +
        "Host header is automatically set. The URL must be an absolute path.")]
    public class ForwardAction : Action
    {
        public ForwardAction(string url)
        {
            Url = url;
        }

        [ActionDistinctive]
        public string Url { get; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Forward request to {Url}".Trim();

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null)
                return default;

            var url = Url.EvaluateVariable(context);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new InvalidOperationException("Provided URL is not a valid one. Must be an absolute URI.");

            var originalPath = exchange.Request.Header.Path.ToString();

            if (Uri.TryCreate(originalPath,
                    UriKind.Absolute, out var path))
                originalPath = path.PathAndQuery;

            string finalPath;

            if (uri.PathAndQuery == "/")
                finalPath = originalPath;
            else {
                finalPath =
                    uri!.PathAndQuery + originalPath;
            }

            var hostName = uri.Host;
            var port = uri.Port;
            var scheme = uri.Scheme;

            exchange.Request.Header.Path = finalPath.AsMemory();
            exchange.Request.Header.Authority = $"{hostName}:{port}".AsMemory();
            exchange.Request.Header.Scheme = scheme.AsMemory();

            exchange.Authority = new Authority(hostName, port, string.Equals(scheme, "https",
                StringComparison.OrdinalIgnoreCase));

            if (connection != null)
                connection.Authority = exchange.Authority;

            context.Authority = exchange.Authority;

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Forward any request to https://www.example.com. ",
                new ForwardAction("https://www.example.com"));
        }
    }
}
