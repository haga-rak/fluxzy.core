// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Events
{
    public class EventWriterConnectionReuseTests
    {
        /// <summary>
        /// A client abort leaves the upstream response body partially read. The
        /// exchange must not complete as reusable, otherwise the H11 pool recycles
        /// a connection still carrying unread body bytes and the next exchange on
        /// it fails parsing them as a response header.
        /// </summary>
        [Fact]
        public async Task AbortedDownload_DoesNotPoisonUpstreamH11Connection()
        {
            var chunk = new byte[64 * 1024];

            await using var setup = await ProxiedHostSetup.Create(
                setting => setting.AddAlterationRulesForAny(new ForceHttp11Action()),
                app =>
                {
                    app.MapGet("/big", async context =>
                    {
                        context.Response.ContentLength = 128L * 1024 * 1024;

                        try
                        {
                            for (var i = 0; i < 2048; i++)
                            {
                                await context.Response.Body.WriteAsync(chunk, context.RequestAborted);
                                await context.Response.Body.FlushAsync(context.RequestAborted);
                            }
                        }
                        catch (Exception)
                        {
                            // aborted by the proxy or the client
                        }
                    });

                    app.MapGet("/ok", () => "hello");
                });

            using (var response = await setup.Client.GetAsync("/big", HttpCompletionOption.ResponseHeadersRead))
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[1024];
                await stream.ReadAsync(buffer);
            }

            // Let the proxy observe the abort and settle the upstream connection
            await Task.Delay(750);

            for (var i = 0; i < 5; i++)
            {
                Assert.Equal("hello", await setup.Client.GetStringAsync("/ok"));
            }
        }
    }
}
