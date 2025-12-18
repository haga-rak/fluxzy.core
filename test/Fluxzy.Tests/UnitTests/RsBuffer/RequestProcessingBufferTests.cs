using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.UnitTests.RsBuffer
{
    public class RequestProcessingBufferTests
    {
        [Theory]
        [MemberData(nameof(GetCheckBufferLimitArgs))]
        public async Task CheckAroundBufferLimit(int expectedSize)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            var url = "https://sandbox.fluxzy.io/ip";

            var dummyHeaderName = ComputeProvisionalHeaderLength(url, out var headerSize);

            var count = 3; 

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            var remainingBuffer = expectedSize - headerSize;

            var headerValue = new string('a', remainingBuffer);

            for (int i = 0; i < count; i++) {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.TryAddWithoutValidation(dummyHeaderName, headerValue);

                var response = await client.SendAsync(requestMessage);
                var responseStream = await response.Content.ReadAsStreamAsync();

                await responseStream.CopyToAsync(Stream.Null);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private static string ComputeProvisionalHeaderLength(string url, out int headerSize)
        {
            var dummyHeaderName = "X-Padding";

            headerSize = "GET  HTTP/1.1\r\n\r\n\r\n".Length + "Host: \r\n".Length;

            headerSize += url.Length;
            headerSize += $"{dummyHeaderName}: \r\n".Length;

            return dummyHeaderName;
        }

        public static IEnumerable<object[]> GetCheckBufferLimitArgs()
        {
            yield return new object[] { 500 };
            yield return new object[] { 5192 };

            var defaultBufferSize = FluxzySharedSetting.RequestProcessingBuffer; 

            var marginCount = 32;

            for (int i = (defaultBufferSize - marginCount); i <= (defaultBufferSize + marginCount); i+=4) {
                
                yield return new object[] { i };
            }
        }
    }
}
