// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Clients.H2.Encoder;

namespace Fluxzy.Core
{
    internal readonly struct DataFrameEntry
    {
        public readonly byte[]? RentedBuffer;
        public readonly int Length;
        public readonly int FlowControlledBytes;
        public readonly IList<HeaderField>? TrailerHeaders;
        public readonly int TrailerStreamIdentifier;

        public DataFrameEntry(byte[] rentedBuffer, int length, int flowControlledBytes)
        {
            RentedBuffer = rentedBuffer;
            Length = length;
            FlowControlledBytes = flowControlledBytes;
            TrailerHeaders = null;
            TrailerStreamIdentifier = 0;
        }

        public DataFrameEntry(IList<HeaderField> trailerHeaders, int trailerStreamIdentifier)
        {
            RentedBuffer = null;
            Length = 0;
            FlowControlledBytes = 0;
            TrailerHeaders = trailerHeaders;
            TrailerStreamIdentifier = trailerStreamIdentifier;
        }
    }
}
