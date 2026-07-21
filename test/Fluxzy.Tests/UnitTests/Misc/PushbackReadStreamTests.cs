// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class PushbackReadStreamTests
    {
        [Fact]
        public void Reads_Pass_Through_When_Nothing_Pushed()
        {
            var inner = new MemoryStream(Encoding.ASCII.GetBytes("abcdef"));
            using var stream = new PushbackReadStream(inner);

            Assert.Equal("abcdef", stream.ReadToEndGreedy());
        }

        [Fact]
        public void Pushed_Bytes_Are_Served_Before_Inner_Stream()
        {
            var inner = new MemoryStream(Encoding.ASCII.GetBytes("world"));
            using var stream = new PushbackReadStream(inner);

            stream.Push(Encoding.ASCII.GetBytes("hello "));

            Assert.Equal("hello world", stream.ReadToEndGreedy());
        }

        [Fact]
        public async Task Pushed_Bytes_Are_Served_Before_Inner_Stream_Async()
        {
            var inner = new MemoryStream(Encoding.ASCII.GetBytes("world"));
            await using var stream = new PushbackReadStream(inner);

            stream.Push(Encoding.ASCII.GetBytes("hello "));

            var result = await stream.ReadToEndGreedyAsync();

            Assert.Equal("hello world", result);
        }

        [Fact]
        public void Push_Reuses_Buffer_Across_Multiple_Rounds()
        {
            var inner = new MemoryStream(Encoding.ASCII.GetBytes("!"));
            using var stream = new PushbackReadStream(inner);

            var builder = new StringBuilder();
            var readBuffer = new byte[64];

            for (var i = 0; i < 1000; i++) {
                stream.Push(Encoding.ASCII.GetBytes($"{i};"));

                var read = stream.Read(readBuffer, 0, readBuffer.Length);
                builder.Append(Encoding.ASCII.GetString(readBuffer, 0, read));

                Assert.Equal(0, stream.PendingLength);
            }

            builder.Append(stream.ReadToEndGreedy());

            var expected = new StringBuilder();

            for (var i = 0; i < 1000; i++) {
                expected.Append($"{i};");
            }

            expected.Append('!');

            Assert.Equal(expected.ToString(), builder.ToString());
        }

        [Fact]
        public void Push_With_Pending_Bytes_Serves_New_Bytes_First()
        {
            // Pushed bytes were read ahead of the pending ones,
            // so stream order is new push then pending remainder
            var inner = new MemoryStream(Encoding.ASCII.GetBytes("D"));
            using var stream = new PushbackReadStream(inner);

            stream.Push(Encoding.ASCII.GetBytes("BC"));
            stream.Push(Encoding.ASCII.GetBytes("A"));

            Assert.Equal(3, stream.PendingLength);
            Assert.Equal("ABCD", stream.ReadToEndGreedy());
        }

        [Fact]
        public void Partial_Reads_Consume_Pending_Bytes_In_Order()
        {
            var inner = new MemoryStream(Encoding.ASCII.GetBytes("efgh"));
            using var stream = new PushbackReadStream(inner);

            stream.Push(Encoding.ASCII.GetBytes("abcd"));

            var buffer = new byte[2];

            Assert.Equal(2, stream.Read(buffer, 0, 2));
            Assert.Equal("ab", Encoding.ASCII.GetString(buffer));

            Assert.Equal(2, stream.Read(buffer, 0, 2));
            Assert.Equal("cd", Encoding.ASCII.GetString(buffer));

            Assert.Equal(0, stream.PendingLength);
            Assert.Equal("efgh", stream.ReadToEndGreedy());
        }
    }
}
