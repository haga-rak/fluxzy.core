// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Utils;

namespace Fluxzy.Clients
{
    /// <summary>
    ///  Helper class for upstream proxy client
    /// </summary>
    internal static class UpstreamProxyManager
    {
        private static readonly byte[] ConnectAnnounce = "CONNECT "u8.ToArray(); 
        private static readonly byte[] Http11 = " HTTP/1.1\r\n"u8.ToArray();
        private static readonly byte[] HostHeader = "Host: "u8.ToArray();
        private static readonly byte[] CrLf = "\r\n"u8.ToArray();
        private static readonly byte[] ProxyAuthorizationHeader = "Proxy-Authorization: "u8.ToArray();
        private static readonly byte[] ProxyConnectionHeader = "Connection: keep-alive\r\n"u8.ToArray();

        public static int WriteConnectHeader(Span<byte> bufferSpan, ConnectConfiguration config)
        {
            var totalWritten = 0;

            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, ConnectAnnounce);
            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, config.Host);
            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, ":");
            totalWritten += MemoryUtility.CopyFormatAndShift(ref bufferSpan, config.Port);
            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, Http11);

            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, HostHeader);
            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, config.Host);
            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, ":");
            totalWritten += MemoryUtility.CopyFormatAndShift(ref bufferSpan, config.Port);
            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, CrLf);

            if (config.ProxyAuthorizationHeader != null) {
                totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, ProxyAuthorizationHeader);
                totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, config.ProxyAuthorizationHeader);
                totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, CrLf);
            }

            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, ProxyConnectionHeader);
            totalWritten += MemoryUtility.CopyAndShift(ref bufferSpan, CrLf);

            return totalWritten;
        }

        /// <summary>
        ///  Connect to the upstream proxy server
        /// </summary>
        /// <param name="config"></param>
        /// <param name="inStream"></param>
        /// <param name="outStream"></param>
        /// <returns></returns>
        public static async ValueTask<UpstreamProxyConnectResult> Connect(ConnectConfiguration config, Stream inStream, Stream outStream)
        {
            // CONNECT 

            var bufferLength = 1024 + config.Host.Length;

            var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);

            try {
                var headerLength = WriteConnectHeader(buffer, config); 
                await inStream.WriteAsync(buffer, 0, headerLength);

                // Read response

                using var rsBuffer = RsBuffer.Allocate(1024); 

                var headerReadResult = await 
                    Http11HeaderBlockReader.GetNext(outStream, rsBuffer, null, null);

                if (headerReadResult.CloseNotify) 
                    return UpstreamProxyConnectResult.InvalidResponse; 

                if (headerReadResult.TotalReadLength != headerReadResult.HeaderLength)
                    return UpstreamProxyConnectResult.InvalidResponse;

                return ReadResponseBlock(in headerReadResult, rsBuffer); 
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static UpstreamProxyConnectResult ReadResponseBlock(in HeaderBlockReadResult headerReadResult, RsBuffer rsBuffer)
        {
            char[]? charBuffer = ArrayPool<char>.Shared.Rent(headerReadResult.HeaderLength);

            try {
                var headerContent = charBuffer.AsMemory(0, headerReadResult.HeaderLength);

                Encoding.ASCII.GetChars(
                    rsBuffer.Memory.Slice(0, headerReadResult.HeaderLength).Span,
                    headerContent.Span);

                var headers = Http11Parser.Read(headerContent, false, true, false);

                var statusCodeHeader = headers
                    .FirstOrDefault(t => t.Name.Span.Equals(":status", StringComparison.Ordinal)); 

                if (statusCodeHeader.Size == 0)
                    return UpstreamProxyConnectResult.InvalidResponse; // No status code header found or invalid header

                if (!int.TryParse(statusCodeHeader.Value.Span, out var statusCode))
                    return UpstreamProxyConnectResult.InvalidResponse; // Invalid status code

                if (statusCode == 407)
                    return UpstreamProxyConnectResult.AuthenticationRequired;

                return statusCode is >= 200 and < 300 ? UpstreamProxyConnectResult.Ok 
                    : UpstreamProxyConnectResult.InvalidStatusCode;
            }
            finally {
                if (charBuffer != null)
                    ArrayPool<char>.Shared.Return(charBuffer);
            }
        }
    }
}
