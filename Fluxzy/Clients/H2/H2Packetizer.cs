// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.H2
{
    public class Packetizer
    {
        /// <summary>
        ///     TODO : need some serious test
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="buffer"></param>
        /// <param name="endStream"></param>
        /// <param name="streamIdentifier"></param>
        /// <param name="maxFrameSize"></param>
        /// <param name="streamDependency"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> PacketizeHeader(
            ReadOnlySpan<byte> rawData,
            Span<byte> buffer, bool endStream, int streamIdentifier,
            int maxFrameSize, int streamDependency = 0)
        {
            var currentWritten = 0;
            var remains = rawData.Length;
            var maxPayload = maxFrameSize - 9;
            var first = true;

            while (remains > 0)
            {
                var writableLength = Math.Min(maxPayload, remains);

                // Build header  here 
                var end = writableLength == remains;

                var frame = H2Frame.BuildHeaderFrameHeader(writableLength, streamIdentifier, first, endStream && end,
                    end);

                frame.Write(buffer.Slice(currentWritten));

                currentWritten += 9;

                var headerFrame = new HeadersFrame(false, 0, false, end, endStream && end, 0, false, streamDependency);

                var body = rawData.Slice(rawData.Length - remains, writableLength);

                currentWritten += headerFrame.Write(buffer.Slice(currentWritten), body);

                first = false;

                remains -= writableLength;
            }

            return buffer.Slice(0, currentWritten);
        }
    }
}
