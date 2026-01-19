// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Extensions;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Extension methods for blocking requests with various HTTP responses.
    /// </summary>
    public static class BlockingExtensions
    {
        /// <summary>
        ///     Reject the request with HTTP 403 Forbidden.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <returns>A configured filter builder for chaining.</returns>
        public static IConfigureFilterBuilder Reject(this IConfigureActionBuilder actionBuilder)
        {
            actionBuilder.Do(new RejectAction());
            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        ///     Reject the request with a custom HTTP status code.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="statusCode">The HTTP status code to return (e.g., 403, 404, 502).</param>
        /// <returns>A configured filter builder for chaining.</returns>
        public static IConfigureFilterBuilder Reject(this IConfigureActionBuilder actionBuilder, int statusCode)
        {
            actionBuilder.Do(new RejectWithStatusCodeAction(statusCode));
            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        ///     Reject the request with a custom HTTP status code and message.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="message">The response body message.</param>
        /// <param name="contentType">The content type of the message (default: text/plain).</param>
        /// <returns>A configured filter builder for chaining.</returns>
        public static IConfigureFilterBuilder Reject(
            this IConfigureActionBuilder actionBuilder,
            int statusCode,
            string message,
            string contentType = "text/plain")
        {
            actionBuilder.Do(new RejectWithMessageAction(statusCode, message, contentType));
            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }
    }
}
