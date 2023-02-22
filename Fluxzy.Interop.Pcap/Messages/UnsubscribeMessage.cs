using System.IO;

namespace Fluxzy.Capturing.Messages;

public readonly struct UnsubscribeMessage
{
    public UnsubscribeMessage(long key)
    {
        Key = key;
    }

    public long Key { get;  }
        
    public static UnsubscribeMessage FromReader(BinaryReader reader)
    {
        var key = reader.ReadInt64();
        return new UnsubscribeMessage(key);
    }
        
    public void Write(BinaryWriter writer)
    {
        writer.Write(Key);
    }
}