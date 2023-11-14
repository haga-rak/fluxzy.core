namespace Fluxzy.Core.Pcap.Messages
{
    public enum MessageType : byte
    {
        Exit = 0,
        Subscribe = 1,
        Unsubscribe = 2,
        Include = 3,
        StoreKey = 4,
        Ready = 100,
        Flush = 101,
        ClearAll = 102
    }
}
