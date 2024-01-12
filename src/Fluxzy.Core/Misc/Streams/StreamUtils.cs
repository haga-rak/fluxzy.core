// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

namespace Fluxzy.Misc.Streams
{
    internal static class StreamUtils
    {
        public static Stream EmptyStream => new MemoryStream(Array.Empty<byte>());
    }
}
