﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.H2
{
    internal interface IHeaderEncoder
    {
        HPackEncoder Encoder { get; }

        HPackDecoder Decoder { get; }

        /// <summary>
        ///     InternalApply header + hpack to headerbuffer
        /// </summary>
        /// <param name="encodingJob"></param>
        /// <param name="destinationBuffer"></param>
        /// <param name="endStream"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, RsBuffer destinationBuffer, bool endStream);

        /// <summary>
        ///     Remove hpack
        /// </summary>
        /// <param name="encodedBuffer"></param>
        /// <param name="destinationBuffer"></param>
        /// <returns></returns>
        ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer);
    }
}
