// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Echoes.H2.Tests.Tools
{
    public static class AssertionHelper
    {
        /// <summary>
        /// Assert that response corresponds to request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestHashBodyHash"></param>
        /// <param name="response"></param>
        public static async Task ValidateCheck(
            HttpRequestMessage request, string?  requestHashBodyHash,
            HttpResponseMessage response)
        {
            var checkResult = await response.GetCheckResult(); 

            foreach (var header in request.Headers)
            {
                foreach (var headerValue in header.Value)
                {
                    Assert.True(
                        checkResult.Headers != null && checkResult.Headers.Any(
                            h => h.Name.Equals(header.Key, StringComparison.OrdinalIgnoreCase)
                                 && h.Value.Equals(headerValue, StringComparison.OrdinalIgnoreCase)));
                }
            }

            if (!string.IsNullOrWhiteSpace(requestHashBodyHash))
            {
                Assert.Equal(checkResult.RequestContent.Hash, requestHashBodyHash);
            }
        }
    }
}