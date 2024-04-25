// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class Expect100ContinueTests
    {
        private static string GetFromBase64(string rawValue)
        {
            return Encoding.UTF8.GetString(System.Convert.FromBase64String(rawValue));
        }

        [Fact]
        public async Task Should_Get_Response_Event_If_Server_Sent_100_Without_Expected_Header()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            var url = GetFromBase64(
                "aHR0cHM6Ly9kZXZpcy1hc3N1cmFuY2Utc2FudGUuZ21mLmZyL3B1YmxpYy9wYWdlcy9kZXZpcy9EU1AxLmZhY2U=");

            var str = GetFromBase64(
                "al9pZHQ2Mz1qX2lkdDYzJmpfaWR0NjMlM0FjdXN0b21SYWRpb0NpdmlsaXRlPU1PTlNJRVVSJmpfaWR0NjMlM0Fub209TU5UUk5HU1Qmal9pZHQ2MyUzQXByZW5vbT1QWUdNQUxJT04mal9pZHQ2MyUzQW5haXNzYW5jZT0xNSUyRjAxJTJGMTk4NiZqX2lkdDYzJTNBcG9zdGFsPTkyMzAwJmpfaWR0NjMlM0FwaG9uZT0wMTExMTExMTExJmpfaWR0NjMlM0FlbWFpbD1nbWYlNDAyYmVmZmljaWVudC5jb20mal9pZHQ2MyUzQWN1c3RvbVJhZGlvU3RvcFB1Yj1OT04mamF2YXguZmFjZXMuVmlld1N0YXRlPS0yOTA1MDk3NTk1NTQ1MzA3NDc5JTNBMTkyMTAyOTU4NDUyODQxMDE5NCZqX2lkdDYzJTNBal9pZHQxMDE9al9pZHQ2MyUzQWpfaWR0MTAx");

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);

            requestMessage.Content = new StringContent(str, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.SendAsync(requestMessage);

            var statusCode = response.StatusCode;

            Assert.NotEqual(528, (int)statusCode);
        }
    }
}
