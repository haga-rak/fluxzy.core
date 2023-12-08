// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Extensions;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Mock completely a response.
    /// </summary>
    [ActionMetadata("Reply with a pre-made response from a raw text or file")]
    public class MockedResponseAction : Action
    {
        public MockedResponseAction(MockedResponseContent?  response)
        {
            Response = response ?? new MockedResponseContent(200, BodyContent.CreateFromString(""));
        }

        /// <summary>
        ///     The response
        /// </summary>
        [ActionDistinctive(Expand = true)]
        public MockedResponseContent Response { get; set; } 

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Mock response";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.PreMadeResponse = Response;

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            {
                var bodyContent = BodyContent.CreateFromString("{ \"result\": true }");
                bodyContent.Type = BodyType.Json;

                yield return new ActionExample("Mock a response with a raw text",
                    new MockedResponseAction(new MockedResponseContent(200, bodyContent)
                    {
                        Headers = {
                            new ("DNT", "1"),
                            new ("X-Custom-Header", "Custom-HeaderValue"),
                        },
                    }));
            }

            {
                var bodyContent = BodyContent.CreateFromFile("/path/to/my/response.data"); 
                bodyContent.Type = BodyType.Binary;

                yield return new ActionExample("Mock a response with a file.",
                    new MockedResponseAction(new MockedResponseContent(404, bodyContent)
                    {
                        Headers = {
                            new ("Server", "Fluxzy"),
                            new ("X-Custom-Header-2", "Custom-HeaderValue-2"),
                        },
                    }));
            }
        }

        public static MockedResponseAction BuildDefaultInstance()
        {
            var bodyContent = BodyContent.CreateFromString("{ \"result\": true }");

            bodyContent.Type = BodyType.Json;

            return new MockedResponseAction(new MockedResponseContent(200, bodyContent)
            {
                Headers = {
                    new ("DNT", "1"),
                    new ("X-Custom-Header", "Custom-HeaderValue")
                },
            });
        }
    }


    public static class MockedResponseExtensions
    {
        /// <summary>
        /// Generates a mocked response for a file and configures it as a response action for a given action builder.
        /// </summary>
        /// <param name="actionBuilder">The action builder to configure.</param>
        /// <param name="fileName">The name of the file to generate a response from.</param>
        /// <param name="statusCode">The HTTP status code for the response (default is 200).</param>
        /// <param name="contentType">The content type of the response (default is null).</param>
        /// <param name="headers">Additional headers to be included in the response (default is null).</param>
        /// <returns>An instance of <see cref="IConfigureFilterBuilder"/> for further configuration.</returns>
        public static IConfigureFilterBuilder ReplyFile(
            this IConfigureActionBuilder actionBuilder, string fileName,
            int statusCode = 200, string? contentType = null, params (string, string)[] headers)
        {
            var content = MockedResponseContent.CreateFromFile(fileName, statusCode, contentType);

            foreach (var (name, value) in headers)
                content.Headers.Add(new MockedResponseHeader(name, value));

            actionBuilder.Do(new MockedResponseAction(content));
            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        /// Configures the simulated response with text content.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="text">The text content to be returned.</param>
        /// <param name="statusCode">The HTTP status code of the response. Default is 200.</param>
        /// <param name="contentType">The content type of the response. Default is "text/plain".</param>
        /// <param name="headers">The headers to be added to the response.</param>
        /// <returns>A configured filter builder.</returns>
        public static IConfigureFilterBuilder ReplyText(
            this IConfigureActionBuilder actionBuilder, string text,
            int statusCode = 200, string? contentType = "text/plain", params (string, string)[] headers)
        {
            var content = MockedResponseContent.CreateFromString(text, statusCode, contentType ?? "");

            foreach (var (name, value) in headers)
                content.Headers.Add(new MockedResponseHeader(name, value));

            actionBuilder.Do(new MockedResponseAction(content));
            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        /// Configures a mocked response that returns a byte array content.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="bytes">The byte array content to be returned.</param>
        /// <param name="statusCode">The HTTP status code to be returned. The default value is 200.</param>
        /// <param name="contentType">The content type of the response. The default value is "application/octet-stream".</param>
        /// <param name="headers">The additional headers to be included in the response.</param>
        /// <returns>The configure filter builder.</returns>
        public static IConfigureFilterBuilder ReplyByteArray(
            this IConfigureActionBuilder actionBuilder, byte[] bytes,
            int statusCode = 200, string? contentType = "application/octet-stream", params (string, string)[] headers)
        {
            var content = MockedResponseContent.CreateFromByteArray(bytes, statusCode, contentType ?? "");

            foreach (var (name, value) in headers)
                content.Headers.Add(new MockedResponseHeader(name, value));

            actionBuilder.Do(new MockedResponseAction(content));
            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        /// Sets up a mocked response with JSON content.
        /// </summary>
        /// <param name="actionBuilder">The action builder.</param>
        /// <param name="json">The JSON content to be returned.</param>
        /// <param name="statusCode">The HTTP status code of the response. Default is 200.</param>
        /// <param name="headers">The custom headers to be included in the response.</param>
        /// <returns>A configure filter builder for further configuration.</returns>
        public static IConfigureFilterBuilder ReplyJson(
            this IConfigureActionBuilder actionBuilder, string json,
            int statusCode = 200, params (string, string)[] headers)
        {
            var content = MockedResponseContent.CreateFromString(json, statusCode, "application/json");

            foreach (var (name, value) in headers)
                content.Headers.Add(new MockedResponseHeader(name, value));

            actionBuilder.Do(new MockedResponseAction(content));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }

        /// <summary>
        /// Generates a mocked response with JSON content from a file and configures the action builder to reply with the generated response.
        /// </summary>
        /// <param name="actionBuilder">The action builder to configure.</param>
        /// <param name="fileName">The name of the file that contains the JSON content.</param>
        /// <param name="statusCode">The status code to be set in the response. Default is 200.</param>
        /// <param name="headers">Additional headers to be included in the response.</param>
        /// <returns>A configure filter builder instance.</returns>
        public static IConfigureFilterBuilder ReplyJsonFile(
            this IConfigureActionBuilder actionBuilder, string fileName,
            int statusCode = 200, params (string, string)[] headers)
        {
            var content = MockedResponseContent.CreateFromFile(fileName, statusCode, "application/json");

            foreach (var (name, value) in headers)
                content.Headers.Add(new MockedResponseHeader(name, value));

            actionBuilder.Do(new MockedResponseAction(content));

            return new ConfigureFilterBuilderBuilder(actionBuilder.Setting);
        }
    }
}
