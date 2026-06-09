// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.Ssl;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Ssl
{
    public class TlsClientHelloParserTests
    {
        // ----- TryReadServerName (pure record parsing) -----

        [Fact]
        public void TryReadServerName_WithSni_ReturnsHost()
        {
            Assert.True(TlsClientHelloParser.TryReadServerName(BuildClientHello("example.com"), out var host));
            Assert.Equal("example.com", host);
        }

        [Fact]
        public void TryReadServerName_NoSniExtension_ReturnsFalse()
        {
            Assert.False(TlsClientHelloParser.TryReadServerName(BuildClientHello(), out var host));
            Assert.Null(host);
        }

        [Fact]
        public void TryReadServerName_FirstHostNameEntryWins_WithMultipleNames()
        {
            Assert.True(TlsClientHelloParser.TryReadServerName(
                BuildClientHello("first.example.com", "second.example.com"), out var host));
            Assert.Equal("first.example.com", host);
        }

        [Fact]
        public void TryReadServerName_SniAmongOtherExtensions_ReturnsHost()
        {
            // SNI is rarely the first extension on a real ClientHello.
            var extensions = Concat(
                Extension(0x002b, new byte[] { 0x02, 0x03, 0x04 }), // supported_versions (dummy payload)
                SniExtension(HostNameEntry("late.example.com")),
                Extension(0x000d, new byte[] { 0x01, 0x02 }));      // signature_algorithms (dummy payload)

            var record = Record(0x01, ClientHelloBody(extensions));

            Assert.True(TlsClientHelloParser.TryReadServerName(record, out var host));
            Assert.Equal("late.example.com", host);
        }

        [Fact]
        public void TryReadServerName_NonHostNameEntrySkipped_ReturnsHostName()
        {
            var list = Concat(NameEntry(0x01, "ignored"), NameEntry(0x00, "real.example.com"));
            var record = Record(0x01, ClientHelloBody(SniExtension(list)));

            Assert.True(TlsClientHelloParser.TryReadServerName(record, out var host));
            Assert.Equal("real.example.com", host);
        }

        [Fact]
        public void TryReadServerName_OnlyNonHostNameEntry_ReturnsFalse()
        {
            var record = Record(0x01, ClientHelloBody(SniExtension(NameEntry(0x01, "not-a-host"))));

            Assert.False(TlsClientHelloParser.TryReadServerName(record, out var host));
            Assert.Null(host);
        }

        [Fact]
        public void TryReadServerName_EmptyHostName_ReturnsFalse()
        {
            var record = Record(0x01, ClientHelloBody(SniExtension(NameEntry(0x00, ""))));

            Assert.False(TlsClientHelloParser.TryReadServerName(record, out var host));
            Assert.Null(host);
        }

        [Fact]
        public void TryReadServerName_ServerHelloMessageType_ReturnsFalse()
        {
            // 0x02 = ServerHello, not a ClientHello.
            var record = Record(0x02, ClientHelloBody(SniExtension(HostNameEntry("example.com"))));

            Assert.False(TlsClientHelloParser.TryReadServerName(record, out var host));
            Assert.Null(host);
        }

        [Fact]
        public void TryReadServerName_IpLiteralSni_IsReturnedVerbatim()
        {
            // The parser does not judge the value; RecoverAsync is what discards IP literals.
            Assert.True(TlsClientHelloParser.TryReadServerName(BuildClientHello("203.0.113.5"), out var host));
            Assert.Equal("203.0.113.5", host);
        }

        [Fact]
        public void TryReadServerName_CorruptServerNameListLength_ReturnsFalse()
        {
            // server_name_list claims 200 bytes but carries only one short entry.
            var record = Record(0x01, ClientHelloBody(SniExtension(HostNameEntry("x.example.com"), declaredListLength: 200)));

            Assert.False(TlsClientHelloParser.TryReadServerName(record, out var host));
            Assert.Null(host);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(20)]
        [InlineData(40)]
        public void TryReadServerName_Truncated_ReturnsFalseWithoutThrowing(int keep)
        {
            var truncated = BuildClientHello("example.com").AsSpan(0, keep).ToArray();

            Assert.False(TlsClientHelloParser.TryReadServerName(truncated, out var host));
            Assert.Null(host);
        }

        [Fact]
        public void TryReadServerName_NotAHandshakeRecord_ReturnsFalse()
        {
            var applicationData = new byte[] { 0x17, 0x03, 0x03, 0x00, 0x05, 1, 2, 3, 4, 5 };

            Assert.False(TlsClientHelloParser.TryReadServerName(applicationData, out var host));
            Assert.Null(host);
        }

        // ----- PeekAsync (stream read + replay) -----

        [Fact]
        public async Task PeekAsync_ReplaysEveryByteAndExtractsSni()
        {
            var record = BuildClientHello("sni.example.com");
            using var stream = new MemoryStream(record);

            var peek = await TlsClientHelloParser.PeekAsync(stream, CancellationToken.None);

            Assert.True(peek.IsTlsHandshake);
            Assert.Equal("sni.example.com", peek.SniHost);
            Assert.Equal(record, peek.ConsumedBytes.ToArray());
        }

        [Fact]
        public async Task PeekAsync_NonTlsFirstByte_StopsImmediately()
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n"));

            var peek = await TlsClientHelloParser.PeekAsync(stream, CancellationToken.None);

            Assert.False(peek.IsTlsHandshake);
            Assert.Null(peek.SniHost);
            Assert.Equal(new[] { (byte) 'G' }, peek.ConsumedBytes.ToArray());
        }

        [Fact]
        public async Task PeekAsync_EmptyStream_ReturnsNotTls()
        {
            using var stream = new MemoryStream(Array.Empty<byte>());

            var peek = await TlsClientHelloParser.PeekAsync(stream, CancellationToken.None);

            Assert.False(peek.IsTlsHandshake);
            Assert.Null(peek.SniHost);
            Assert.Empty(peek.ConsumedBytes.ToArray());
        }

        [Fact]
        public async Task PeekAsync_CanceledToken_ReturnsGracefully()
        {
            using var stream = new MemoryStream(BuildClientHello("example.com"));

            var peek = await TlsClientHelloParser.PeekAsync(stream, new CancellationToken(true));

            Assert.Null(peek.SniHost);
        }

        [Fact]
        public async Task PeekAsync_TruncatedPayload_ReplaysBytesReadWithoutSni()
        {
            // Keep the 5-byte header (with its full record length) but only a slice of the payload.
            var record = BuildClientHello("example.com");
            var truncated = record.AsSpan(0, 15).ToArray();
            using var stream = new MemoryStream(truncated);

            var peek = await TlsClientHelloParser.PeekAsync(stream, CancellationToken.None);

            Assert.True(peek.IsTlsHandshake);
            Assert.Null(peek.SniHost);
            Assert.Equal(truncated, peek.ConsumedBytes.ToArray());
        }

        [Fact]
        public async Task PeekAsync_ChunkedDelivery_ExtractsSni()
        {
            var record = BuildClientHello("chunked.example.com");
            using var stream = new OneByteAtATimeStream(record);

            var peek = await TlsClientHelloParser.PeekAsync(stream, CancellationToken.None);

            Assert.Equal("chunked.example.com", peek.SniHost);
            Assert.Equal(record, peek.ConsumedBytes.ToArray());
        }

        // ----- RecoverAsync (timeout + replay stream + IP-literal filter) -----

        [Fact]
        public async Task RecoverAsync_HostnameSni_ReturnsHostAndReplaysLosslessly()
        {
            var record = BuildClientHello("recover.example.com");
            using var stream = new MemoryStream(record);

            var recovery = await TlsClientHelloParser.RecoverAsync(stream, CancellationToken.None);

            Assert.Equal("recover.example.com", recovery.SniHost);
            Assert.Equal(record, await DrainAsync(recovery.RecomposedStream));
        }

        [Fact]
        public async Task RecoverAsync_IpLiteralSni_ReturnsNullHostButStillReplays()
        {
            var record = BuildClientHello("198.51.100.7");
            using var stream = new MemoryStream(record);

            var recovery = await TlsClientHelloParser.RecoverAsync(stream, CancellationToken.None);

            Assert.Null(recovery.SniHost);
            Assert.Equal(record, await DrainAsync(recovery.RecomposedStream));
        }

        [Fact]
        public async Task RecoverAsync_NoSni_ReturnsNullHost()
        {
            using var stream = new MemoryStream(BuildClientHello());

            var recovery = await TlsClientHelloParser.RecoverAsync(stream, CancellationToken.None);

            Assert.Null(recovery.SniHost);
        }

        // ----- builders -----

        internal static byte[] BuildClientHello(params string?[] serverNames)
        {
            var hasSni = serverNames is { Length: > 0 } && serverNames[0] != null;

            if (!hasSni)
                return Record(0x01, ClientHelloBody(Array.Empty<byte>()));

            var list = Concat(serverNames.Select(n => HostNameEntry(n!)).ToArray());

            return Record(0x01, ClientHelloBody(SniExtension(list)));
        }

        private static byte[] Record(byte messageType, byte[] handshakeBody)
        {
            var handshake = new List<byte> { messageType };
            handshake.AddRange(UInt24(handshakeBody.Length));
            handshake.AddRange(handshakeBody);

            var record = new List<byte> { 0x16, 0x03, 0x01 };
            record.AddRange(UInt16(handshake.Count));
            record.AddRange(handshake);

            return record.ToArray();
        }

        private static byte[] ClientHelloBody(byte[] extensions)
        {
            var body = new List<byte>();
            body.AddRange(new byte[] { 0x03, 0x03 });     // client_version
            body.AddRange(new byte[32]);                  // random
            body.Add(0x00);                               // session_id length
            body.AddRange(UInt16(2));                     // cipher_suites length
            body.AddRange(new byte[] { 0x13, 0x01 });     // one cipher suite
            body.Add(0x01);                               // compression_methods length
            body.Add(0x00);                               // null compression
            body.AddRange(UInt16(extensions.Length));     // extensions length
            body.AddRange(extensions);

            return body.ToArray();
        }

        private static byte[] Extension(int type, byte[] data)
        {
            var extension = new List<byte>();
            extension.AddRange(UInt16(type));
            extension.AddRange(UInt16(data.Length));
            extension.AddRange(data);

            return extension.ToArray();
        }

        private static byte[] SniExtension(byte[] serverNameList, int? declaredListLength = null)
        {
            var data = new List<byte>();
            data.AddRange(UInt16(declaredListLength ?? serverNameList.Length));
            data.AddRange(serverNameList);

            return Extension(0x0000, data.ToArray());
        }

        private static byte[] HostNameEntry(string name) => NameEntry(0x00, name);

        private static byte[] NameEntry(byte nameType, string name)
        {
            var nameBytes = Encoding.ASCII.GetBytes(name);
            var entry = new List<byte> { nameType };
            entry.AddRange(UInt16(nameBytes.Length));
            entry.AddRange(nameBytes);

            return entry.ToArray();
        }

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

        private static byte[] UInt16(int value) => new[] { (byte) (value >> 8), (byte) value };

        private static byte[] UInt24(int value) => new[] { (byte) (value >> 16), (byte) (value >> 8), (byte) value };

        private static async Task<byte[]> DrainAsync(Stream stream)
        {
            using var sink = new MemoryStream();
            await stream.CopyToAsync(sink);
            return sink.ToArray();
        }

        // Returns at most one byte per read to exercise the partial-read reassembly loop.
        private sealed class OneByteAtATimeStream : Stream
        {
            private readonly byte[] _data;
            private int _position;

            public OneByteAtATimeStream(byte[] data) => _data = data;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_position >= _data.Length || count == 0)
                    return 0;

                buffer[offset] = _data[_position++];
                return 1;
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_position >= _data.Length || buffer.Length == 0)
                    return new ValueTask<int>(0);

                buffer.Span[0] = _data[_position++];
                return new ValueTask<int>(1);
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
