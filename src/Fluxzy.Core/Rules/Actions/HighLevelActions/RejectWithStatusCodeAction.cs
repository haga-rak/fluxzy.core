// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Reject the request with a custom HTTP status code.
    /// </summary>
    [ActionMetadata(
        "Block the request and return a custom HTTP error response. " +
        "Allows specifying the status code (e.g., 403, 404, 502) to return to the client. " +
        "The response body will contain the standard reason phrase for the status code.")]
    public class RejectWithStatusCodeAction : Action
    {
        public RejectWithStatusCodeAction()
        {
        }

        public RejectWithStatusCodeAction(int statusCode)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        ///     The HTTP status code to return (e.g., 403, 404, 502).
        /// </summary>
        [ActionDistinctive(Description = "HTTP status code to return (e.g., 403, 404, 502)")]
        public int StatusCode { get; set; } = 403;

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Reject with {StatusCode}";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            var reasonPhrase = ((HttpStatusCode)StatusCode).ToString();
            var bodyContent = Clients.Mock.BodyContent.CreateFromString(reasonPhrase);
            bodyContent.Type = Clients.Mock.BodyType.Text;

            context.PreMadeResponse = new MockedResponseContent(StatusCode, bodyContent);

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Block with 404 Not Found (hide resource existence)",
                new RejectWithStatusCodeAction(404));

            yield return new ActionExample(
                "Block with 502 Bad Gateway (simulate server unavailability)",
                new RejectWithStatusCodeAction(502));
        }
    }
}
