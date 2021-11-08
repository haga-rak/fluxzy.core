using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly struct SettingFrame : IBodyFrame
    {
        public SettingFrame(ReadOnlySpan<byte> bodyBytes)
        {
            SettingIdentifier = (SettingIdentifier) BinaryPrimitives.ReadUInt16BigEndian(bodyBytes);
            Value = BinaryPrimitives.ReadInt32BigEndian(bodyBytes.Slice(2));
            Ack = true; 
        }

        public SettingFrame(SettingIdentifier settingIdentifier, int value)
        {
            SettingIdentifier = settingIdentifier;
            Value = value;
            Ack = false; 
        }

        public SettingFrame(bool ack)
        {
            SettingIdentifier = SettingIdentifier.Undefined;
            Value = 0;
            Ack = true; 
        }

        public bool Ack { get; }
        
        public SettingIdentifier SettingIdentifier { get;  }

        public int Value { get; }

        public int BodyLength => Ack ? 0 : 6;

        public H2FrameType Type => H2FrameType.Settings;

        public void Write(Stream stream)
        {
            if (Ack)
                return; 


            stream.BuWrite_16((ushort)SettingIdentifier);
            stream.BuWrite_32(Value); 
        }
    }


    public enum SettingIdentifier : ushort
    {
        Undefined = 0,
        SettingsHeaderTableSize = 0x1,
        SettingsEnablePush = 0x2,
        SettingsMaxConcurrentStreams = 0x3,
        SettingsInitialWindowSize = 0x4,
        SettingsMaxFrameSize = 0x5,
        SettingsMaxHeaderListSize = 0x6,
    }
}