// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    internal readonly struct PendingHeaderWrite
    {
        public readonly ReadOnlyMemory<char> Http11Header;
        public readonly int StreamIdentifier;
        public readonly bool HasBody;

        public PendingHeaderWrite(ReadOnlyMemory<char> http11Header, int streamIdentifier, bool hasBody)
        {
            Http11Header = http11Header;
            StreamIdentifier = streamIdentifier;
            HasBody = hasBody;
        }
    }
}
