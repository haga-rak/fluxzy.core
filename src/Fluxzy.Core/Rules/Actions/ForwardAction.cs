// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Extensions;

namespace Fluxzy.Rules.Actions
{
    [ActionMetadata(
        "Forward request to a specific URL. This action makes fluxzy act as a reverse proxy. " +
        "Unlike [SpoofDnsAction](https://www.fluxzy.io/rule/item/spoofDnsAction), host header is automatically set and protocol switch is supported (http to https, http/1.1 to h2, ...). The URL must be an absolute path.")]
    public class ForwardAction : Action
    {
        public ForwardAction(string url)
        {
            Url = url;
        }

        [ActionDistinctive]
        public string Url { get; }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

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

    public static class ForwardActionExtensions
    {
        /// <summary>
        /// Forwards the request to the specified URL.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigureActionBuilder"/> object.</param>
        /// <param name="url">The URL to which the request should be forwarded.</param>
        /// <returns>The <see cref="IConfigureFilterBuilder"/> object.</returns>
        public static IConfigureFilterBuilder Forward(this IConfigureActionBuilder builder, string url)
        {
            builder.Do(new ForwardAction(url));

            return new ConfigureFilterBuilderBuilder(builder.Setting); 
        }
    }
}
