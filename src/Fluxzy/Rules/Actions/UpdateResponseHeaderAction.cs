﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Headers;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Update and existing response header. If the header does not exists in the original response, the header will be
    ///     added.
    ///     Use {{previous}} keyword to refer to the original value of the header.
    ///     <strong>Note</strong> Headers that alter the connection behaviour will be ignored.
    /// </summary>
    [ActionMetadata(
        "Update and existing response header. If the header does not exists in the original response, the header will be added.<br/>" +
        "Use {{previous}} keyword to refer to the original value of the header.<br/>" +
        "<strong>Note</strong> Headers that alter the connection behaviour will be ignored.")]
    public class UpdateResponseHeaderAction : Action
    {
        public UpdateResponseHeaderAction(string headerName, string headerValue)
        {
            HeaderName = headerName;
            HeaderValue = headerValue;
        }

        /// <summary>
        ///     Header name
        /// </summary>
        [ActionDistinctive]
        public string HeaderName { get; set; }

        /// <summary>
        ///     Header value
        /// </summary>
        [ActionDistinctive]
        public string HeaderValue { get; set; }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription => $"Update response header {HeaderName}".Trim();

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.ResponseHeaderAlterations.Add(new HeaderAlterationReplace(HeaderName, HeaderValue));

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Update the Server header",
                new UpdateResponseHeaderAction("Server", "Fluxzy"));
        }
    }
}
