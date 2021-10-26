using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public readonly struct SettingFrame : IFixedSizeFrame
    {
        public SettingFrame(ReadOnlySpan<byte> bodyBytes)
        {
            SettingIdentifier = (SettingIdentifier) BinaryPrimitives.ReadUInt16BigEndian(bodyBytes);
            Value = BinaryPrimitives.ReadInt32BigEndian(bodyBytes.Slice(2));
        }

        public SettingFrame(SettingIdentifier settingIdentifier, int value)
        {
            SettingIdentifier = settingIdentifier;
            Value = value;
        }
        
        public SettingIdentifier SettingIdentifier { get;  }

        public int Value { get; }

        public int Length => 6;

        public H2FrameType Type => H2FrameType.Settings;

        public void Write(Stream stream)
        {
            stream.BuWrite_16((ushort)SettingIdentifier);
            stream.BuWrite_32(Value); 
        }
    }


    public enum SettingIdentifier : ushort
    {
        SettingsHeaderTableSize = 0x1,
        SettingsEnablePush = 0x2,
        SettingsMaxConcurrentStreams = 0x3,
        SettingsInitialWindowSize = 0x4,
        SettingsMaxFrameSize = 0x5,
        SettingsMaxHeaderListSize = 0x6,
    }
}