// Copyright © 2021 Haga Rakotoharivelo

using System;

namespace Fluxzy.Clients.H2
{
    public readonly ref struct HeaderEncodingJob
    {
        public HeaderEncodingJob(ReadOnlyMemory<char> data, int streamIdentifier, int streamDependency)
        {
            Data = data;
            StreamIdentifier = streamIdentifier;
            StreamDependency = streamDependency;
        }

        public ReadOnlyMemory<char> Data { get;  }

        public int StreamIdentifier { get;  }

        public int StreamDependency { get;  }
    }
}