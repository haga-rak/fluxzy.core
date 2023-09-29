// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Mock completely a response.
    /// </summary>
    [ActionMetadata("Reply with a pre-made response from a raw text or file")]
    public class MockedResponseAction : Action
    {
        public MockedResponseAction(MockedResponseContent response)
        {
            Response = response;
        }

        /// <summary>
        ///     The response
        /// </summary>
        [ActionDistinctive(Expand = true)]
        public MockedResponseContent Response { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Full response substitution";

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
                            ["DNT"] = "1",
                            ["X-Custom-Header"] = "Custom-HeaderValue"
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
                            ["Server"] = "Fluxzy",
                            ["X-Custom-Header-2"] = "Custom-HeaderValue-2"
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
                    ["DN"] = "1",
                    ["X-Custom-Header"] = "Custom-HeaderValue"
                },
            });
        }
    }
}
