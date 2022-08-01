namespace Echoes.Clients.H2.Encoder.HPack
{
    public enum HeaderFieldType : byte
    {
        IndexedHeaderField = 1,
        LiteralHeaderFieldIncrementalIndexingExistingName ,
        LiteralHeaderFieldIncrementalIndexingWithName,
        LiteralHeaderFieldWithoutIndexingExistingName, 
        LiteralHeaderFieldWithoutIndexingWithName,
        LiteralHeaderFieldNeverIndexExistingName,
        LiteralHeaderFieldNeverIndexWithName,
        DynamicTableSizeUpdate
    }
}