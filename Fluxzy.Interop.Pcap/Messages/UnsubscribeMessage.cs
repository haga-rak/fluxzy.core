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

    public bool Equals(UnsubscribeMessage other)
    {
        return Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
        return obj is UnsubscribeMessage other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }

}