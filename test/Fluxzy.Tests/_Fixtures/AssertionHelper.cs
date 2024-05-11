// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests._Fixtures
{
    public static class AssertionHelper
    {
        /// <summary>
        ///     Assert that response corresponds to request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestHashBodyHash"></param>
        /// <param name="response"></param>
        /// <param name="token"></param>
        public static async Task ValidateCheck(
            HttpRequestMessage request, string? requestHashBodyHash,
            HttpResponseMessage response, CancellationToken token = default)
        {
            var checkResult = await response.GetCheckResult(token);

            foreach (var header in request.Headers.Where(h => h.Key != "fluxzy")) {
                foreach (var headerValue in header.Value) {
                    Assert.True(
                        checkResult.Headers != null && checkResult.Headers.Any(
                            h => h.Name.Equals(header.Key, StringComparison.OrdinalIgnoreCase)
                                 && h.Value.Equals(headerValue, StringComparison.OrdinalIgnoreCase)));
                }
            }

            if (!string.IsNullOrWhiteSpace(requestHashBodyHash))
                Assert.Equal(checkResult.RequestContent.Hash, requestHashBodyHash);
        }
    }
}
