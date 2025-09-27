// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Fluxzy.Tests._Fixtures
{
    public static class AssertHelpers
    {
        public static MockResponse ControlHeaders(
            string contentText, HttpRequestMessage requestMessage,
            int responseSize = -1)
        {
            var binResponse = JsonSerializer.Deserialize<MockResponse>(contentText)!;

            if (responseSize >= 0 && binResponse.Headers!.ContainsKey("Content-Length")) {
                var contentLength = int.Parse(binResponse.Headers["Content-Length"]);
                Assert.Equal(responseSize, contentLength);
            }

            foreach (var header in requestMessage.Headers) {
                if (HttpConstants.PermanentHeaders.Contains(header.Key))
                    continue;

                Assert.True(binResponse.Headers!.TryGetValue(header.Key, out var responseValue));
                Assert.Equal(string.Join(", ", header.Value), responseValue);
            }

            return binResponse;
        }

        public static MockResponse ControlBody(this MockResponse response, string? expectedHash)
        {
            var data = response.Data;

            if (string.IsNullOrWhiteSpace(data))
                return response;

            if (string.IsNullOrWhiteSpace(expectedHash))
                return response;

            var prefix = "data:application/octet-stream;base64,";

            if (!data.StartsWith(prefix))
                return response;

            var base64Encoded = data.AsMemory(prefix.Length);

            using var resultCrypto = new MemoryStream();

            using var crypto = new CryptoStream(new DecodingStream(base64Encoded), new FromBase64Transform(),
                CryptoStreamMode.Read);

            using var sha1 = SHA1.Create();
            using var cryptoHash = new CryptoStream(resultCrypto, sha1, CryptoStreamMode.Write);

            crypto.CopyTo(cryptoHash);
            cryptoHash.FlushFinalBlock();

            var arrayResult = resultCrypto.ToArray();
            var hash = MockedBinaryUtilities.GetStringSha256Hash(arrayResult);

            Assert.Equal(expectedHash, hash);

            return response;
        }

        public class DecodingStream : Stream
        {
            private readonly Encoding _encoding;
            private ReadOnlyMemory<char> _array;

            public DecodingStream(ReadOnlyMemory<char> array)
            {
                _array = array;
                _encoding = Encoding.ASCII;
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position {
                get => throw new NotSupportedException();

                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(Span<byte> buffer)
            {
                var maxReadable = Math.Min(buffer.Length, _array.Length);

                if (maxReadable == 0)
                    return 0;

                var currentRead = _encoding.GetBytes(_array.Span.Slice(0, maxReadable), buffer);
                _array = _array.Slice(currentRead);

                return currentRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(new Span<byte>(buffer, offset, count));
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }

}
