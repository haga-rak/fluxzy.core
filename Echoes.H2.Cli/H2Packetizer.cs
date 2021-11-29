// Copyright � 2021 Haga Rakotoharivelo

using System;
using System.IO;

namespace Echoes.H2.Cli
{
    public class Packetizer
    {
        /// <summary>
        /// TODO : need some serious test 
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="buffer"></param>
        /// <param name="streamIdentifier"></param>
        /// <param name="maxFrameSize"></param>
        /// <param name="streamDependency"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> Packetize(
            ReadOnlySpan<byte> rawData,
            Span<byte> buffer, int streamIdentifier, 
            int maxFrameSize, int streamDependency = 0)
        {
            int currentWritten = 0;
            int remains = rawData.Length;
            int maxPayload = maxFrameSize - 9;
            bool first = true; 

            while (remains > 0)
            {
                var writtableLength = Math.Max(maxPayload, remains);

                // Build header  here 
                var end = writtableLength == remains;

                var frame = H2Frame.BuildHeaderFrameHeader(writtableLength, streamIdentifier, first, false, end);

                frame.Write(buffer.Slice(currentWritten));
                
                currentWritten += 9;

                var headerFrame = new HeaderFrame(false, 0, false, end, false, 0, false, streamDependency
                    );

                currentWritten += headerFrame.Write(buffer.Slice(currentWritten), rawData.Slice(rawData.Length - remains, writtableLength));

                first = false; 

                remains -= writtableLength; 
            }

            return buffer.Slice(0, currentWritten); 
        }
    }
}