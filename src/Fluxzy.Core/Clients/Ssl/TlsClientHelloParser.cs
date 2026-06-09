// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients.Ssl
{
    /// <summary>Recomposed stream (peeked bytes replayed) plus the usable SNI host, if any.</summary>
    internal readonly struct SniRecovery
    {
        public SniRecovery(Stream recomposedStream, string? sniHost)
        {
            RecomposedStream = recomposedStream;
            SniHost = sniHost;
        }

        public Stream RecomposedStream { get; }

        public string? SniHost { get; }
    }

    /// <summary><see cref="ConsumedBytes"/> are every byte read and must be replayed into the handshake.</summary>
    internal readonly struct ClientHelloPeek
    {
        public ClientHelloPeek(bool isTlsHandshake, string? sniHost, ReadOnlyMemory<byte> consumedBytes)
        {
            IsTlsHandshake = isTlsHandshake;
            SniHost = sniHost;
            ConsumedBytes = consumedBytes;
        }

        public bool IsTlsHandshake { get; }

        public string? SniHost { get; }

        public ReadOnlyMemory<byte> ConsumedBytes { get; }
    }

    /// <summary>Reads the SNI host from a client's ClientHello. Never throws; malformed input yields null.</summary>
    internal static class TlsClientHelloParser
    {
        private const byte HandshakeContentType = 0x16;
        private const byte ClientHelloType = 0x01;
        private const int ServerNameExtensionType = 0x0000;
        private const byte HostNameType = 0x00;

        // Bounds the wait for a silent client (e.g. a non-TLS blind tunnel) so it forwards instead of stalling.
        private const int PeekTimeoutMilliseconds = 5000;

        /// <summary>Peek the ClientHello (bounded) and return a replaying stream plus the usable SNI host.</summary>
        public static async Task<SniRecovery> RecoverAsync(Stream stream, CancellationToken token)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutSource.CancelAfter(PeekTimeoutMilliseconds);

            var peek = await PeekAsync(stream, timeoutSource.Token).ConfigureAwait(false);

            // Replay peeked bytes on read; keep writes on the socket so the stream stays read/write.
            var replayed = new CombinedReadonlyStream(false, peek.ConsumedBytes.Span, stream);
            var recomposed = new RecomposedStream(replayed, stream);

            var host = !string.IsNullOrWhiteSpace(peek.SniHost) && !IPAddress.TryParse(peek.SniHost, out _)
                ? peek.SniHost
                : null;

            return new SniRecovery(recomposed, host);
        }

        /// <summary>Read the leading TLS record and extract its SNI, returning every byte read for replay.</summary>
        public static async Task<ClientHelloPeek> PeekAsync(Stream stream, CancellationToken token)
        {
            // The first byte alone says whether this is TLS, so a non-TLS client only loses that byte.
            var header = new byte[5];
            var headerRead = await ReadUpToAsync(stream, header.AsMemory(0, 1), token).ConfigureAwait(false);

            if (headerRead == 0)
                return new ClientHelloPeek(false, null, ReadOnlyMemory<byte>.Empty);

            if (header[0] != HandshakeContentType)
                return new ClientHelloPeek(false, null, header.AsMemory(0, 1));

            headerRead += await ReadUpToAsync(stream, header.AsMemory(1, 4), token).ConfigureAwait(false);

            if (headerRead < header.Length)
                return new ClientHelloPeek(true, null, header.AsMemory(0, headerRead));

            var recordLength = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(3, 2));

            // The record buffer doubles as the bytes we replay, so it is sized exactly and read once.
            // A ClientHello fits in a single record in practice; a fragmented one fails the parse bounds
            // check below and falls back to the connect authority.
            var record = new byte[5 + recordLength];
            header.CopyTo(record, 0);

            var payloadRead = await ReadUpToAsync(stream, record.AsMemory(5, recordLength), token).ConfigureAwait(false);

            var consumed = payloadRead == recordLength ? record : record.AsSpan(0, 5 + payloadRead).ToArray();

            TryReadServerName(consumed, out var host);

            return new ClientHelloPeek(true, host, consumed);
        }

        /// <summary>Parse the SNI host from a single complete ClientHello record (record header included).</summary>
        public static bool TryReadServerName(ReadOnlySpan<byte> tlsRecord, out string? hostName)
        {
            hostName = null;

            if (tlsRecord.Length < 5 || tlsRecord[0] != HandshakeContentType)
                return false;

            var recordLength = BinaryPrimitives.ReadUInt16BigEndian(tlsRecord.Slice(3, 2));
            var payload = tlsRecord.Slice(5);

            if (payload.Length < recordLength)
                return false;

            return TryReadServerNameFromHandshake(payload.Slice(0, recordLength), out hostName);
        }

        private static bool TryReadServerNameFromHandshake(ReadOnlySpan<byte> handshake, out string? hostName)
        {
            hostName = null;

            if (handshake.Length < 4 || handshake[0] != ClientHelloType)
                return false;

            var bodyLength = (handshake[1] << 16) | (handshake[2] << 8) | handshake[3];
            var body = handshake.Slice(4);

            if (body.Length < bodyLength)
                return false;

            body = body.Slice(0, bodyLength);

            var pos = 0;

            // client_version (2) + random (32)
            if (!Skip(ref pos, body.Length, 2 + 32))
                return false;

            // session_id: 1-byte length prefix
            if (pos >= body.Length)
                return false;

            if (!Skip(ref pos, body.Length, body[pos] + 1))
                return false;

            // cipher_suites: 2-byte length prefix
            if (pos + 2 > body.Length)
                return false;

            var cipherSuitesLength = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(pos, 2));

            if (!Skip(ref pos, body.Length, cipherSuitesLength + 2))
                return false;

            // compression_methods: 1-byte length prefix
            if (pos >= body.Length)
                return false;

            if (!Skip(ref pos, body.Length, body[pos] + 1))
                return false;

            // extensions: 2-byte length prefix (absent on very old hellos => no SNI)
            if (pos + 2 > body.Length)
                return false;

            var extensionsLength = BinaryPrimitives.ReadUInt16BigEndian(body.Slice(pos, 2));
            pos += 2;

            if (pos + extensionsLength > body.Length)
                return false;

            var extensions = body.Slice(pos, extensionsLength);
            var extPos = 0;

            while (extPos + 4 <= extensions.Length) {
                var extensionType = BinaryPrimitives.ReadUInt16BigEndian(extensions.Slice(extPos, 2));
                var extensionLength = BinaryPrimitives.ReadUInt16BigEndian(extensions.Slice(extPos + 2, 2));
                extPos += 4;

                if (extPos + extensionLength > extensions.Length)
                    return false;

                var extensionData = extensions.Slice(extPos, extensionLength);
                extPos += extensionLength;

                if (extensionType != ServerNameExtensionType)
                    continue;

                return TryReadHostNameEntry(extensionData, out hostName);
            }

            return false;
        }

        private static bool TryReadHostNameEntry(ReadOnlySpan<byte> serverNameExtension, out string? hostName)
        {
            hostName = null;

            // server_name_list: 2-byte length prefix
            if (serverNameExtension.Length < 2)
                return false;

            var listLength = BinaryPrimitives.ReadUInt16BigEndian(serverNameExtension.Slice(0, 2));
            var list = serverNameExtension.Slice(2);

            if (list.Length < listLength)
                return false;

            list = list.Slice(0, listLength);

            var pos = 0;

            while (pos + 3 <= list.Length) {
                var nameType = list[pos];
                var nameLength = BinaryPrimitives.ReadUInt16BigEndian(list.Slice(pos + 1, 2));
                pos += 3;

                if (pos + nameLength > list.Length)
                    return false;

                if (nameType == HostNameType) {
                    if (nameLength == 0)
                        return false;

                    // host_name is carried as ASCII (IDNA a-labels) on the wire.
                    hostName = Encoding.ASCII.GetString(list.Slice(pos, nameLength));

                    return true;
                }

                pos += nameLength;
            }

            return false;
        }

        private static bool Skip(ref int pos, int length, int by)
        {
            if (by < 0)
                return false;

            pos += by;

            return pos <= length;
        }

        // Reads up to buffer.Length bytes, returning how many were read. EOF, IO error and the peek
        // timeout all just stop early; whatever was read is still returned for replay.
        private static async Task<int> ReadUpToAsync(Stream stream, Memory<byte> buffer, CancellationToken token)
        {
            var offset = 0;

            try {
                while (offset < buffer.Length) {
                    var read = await stream.ReadAsync(buffer.Slice(offset), token).ConfigureAwait(false);

                    if (read == 0)
                        break;

                    offset += read;
                }
            }
            catch {
                // Timeout/IO: return what was read so far.
            }

            return offset;
        }
    }
}
