// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Reject the request with a custom HTTP status code and a custom body message.
    /// </summary>
    [ActionMetadata(
        "Block the request with a custom HTTP error response including a body message. " +
        "Useful for providing detailed blocking reasons to end users. " +
        "Supports text/plain, text/html, and application/json content types.")]
    public class RejectWithMessageAction : Action
    {
        [JsonConstructor]
        public RejectWithMessageAction()
        {
        }

        public RejectWithMessageAction(int statusCode, string message, string contentType = "text/plain")
        {
            StatusCode = statusCode;
            Message = message;
            ContentType = contentType;
        }

        /// <summary>
        ///     The HTTP status code to return.
        /// </summary>
        [ActionDistinctive(Description = "HTTP status code to return")]
        public int StatusCode { get; set; } = 403;

        /// <summary>
        ///     The response body message.
        /// </summary>
        [ActionDistinctive(Description = "Response body message")]
        public string Message { get; set; } = "Request blocked by proxy";

        /// <summary>
        ///     The content type of the message (text/plain, text/html, application/json).
        /// </summary>
        [ActionDistinctive(Description = "Content type of the message (text/plain, text/html, application/json)")]
        public string ContentType { get; set; } = "text/plain";

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Reject with {StatusCode}";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            var bodyContent = Clients.Mock.BodyContent.CreateFromString(Message);
            bodyContent.Type = GetBodyType(ContentType);

            var response = new MockedResponseContent(StatusCode, bodyContent);

            if (!string.IsNullOrWhiteSpace(ContentType))
            {
                response.Headers.Add(new MockedResponseHeader("Content-Type", ContentType));
            }

            context.PreMadeResponse = response;

            return default;
        }

        private static BodyType GetBodyType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return BodyType.Text;

            var lowerContentType = contentType.ToLowerInvariant();

            if (lowerContentType.Contains("json"))
                return BodyType.Json;

            if (lowerContentType.Contains("html"))
                return BodyType.Html;

            if (lowerContentType.Contains("xml"))
                return BodyType.Xml;

            return BodyType.Text;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Block with a plain text message",
                new RejectWithMessageAction(403, "Access to this site is blocked by corporate policy"));

            yield return new ActionExample(
                "Block with an HTML page",
                new RejectWithMessageAction(403,
                    "<html><body><h1>Blocked</h1><p>This site has been blocked for security reasons.</p></body></html>",
                    "text/html"));

            yield return new ActionExample(
                "Block with a JSON response (for APIs)",
                new RejectWithMessageAction(403,
                    "{\"error\": \"forbidden\", \"message\": \"This endpoint is blocked\"}",
                    "application/json"));
        }
    }
}
