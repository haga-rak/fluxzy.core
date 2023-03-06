// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    public enum HeaderFieldType : byte
    {
        IndexedHeaderField = 1,
        LiteralHeaderFieldIncrementalIndexingExistingName,
        LiteralHeaderFieldIncrementalIndexingWithName,
        LiteralHeaderFieldWithoutIndexingExistingName,
        LiteralHeaderFieldWithoutIndexingWithName,
        LiteralHeaderFieldNeverIndexExistingName,
        LiteralHeaderFieldNeverIndexWithName,
        DynamicTableSizeUpdate
    }
}
