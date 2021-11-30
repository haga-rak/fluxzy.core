using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly ref struct SettingFrame
    {
        public SettingFrame(ReadOnlySpan<byte> bodyBytes, HeaderFlags flags)
        {
            if ((flags & HeaderFlags.Ack) != 0)
            {
                Ack = true;
                SettingIdentifier = SettingIdentifier.Undefined;
                Value = 0; 
            }
            else
            {
                SettingIdentifier = (SettingIdentifier)BinaryPrimitives.ReadUInt16BigEndian(bodyBytes);
                Value = BinaryPrimitives.ReadInt32BigEndian(bodyBytes.Slice(2));
                Ack = false; 
            }
        }

        public SettingFrame(bool ack)
        {
            SettingIdentifier = SettingIdentifier.Undefined;
            Value = 0;
            Ack = true; 
        }

        public SettingFrame(SettingIdentifier settingIdentifier, int value)
        {
            SettingIdentifier = settingIdentifier;
            Value = value;
            Ack = false;
        }

        public bool Ack { get; }
        
        public SettingIdentifier SettingIdentifier { get;  }

        public int Value { get; }

        public int Write(Span<byte> buffer, ReadOnlySpan<byte> payload = default)
        {
            var offset = 
                H2Frame.Write(buffer, BodyLength, H2FrameType.Settings, Ack ? HeaderFlags.Ack : HeaderFlags.None, 0);

            if (!Ack)
            {
                buffer = buffer.Slice(offset).BuWrite_16((ushort)SettingIdentifier);
                buffer = buffer.BuWrite_32(Value);

                return 15; 
            }

            return 9; 
        }

        public int BodyLength => Ack ? 0 : 6;

        public void Write(Stream stream)
        {
            if (Ack)
                return; 


            stream.BuWrite_16((ushort)SettingIdentifier);
            stream.BuWrite_32(Value); 
        }

        public override string ToString()
        {
            if (Ack)
                return $"Setting : {Ack}";

            return $"Setting : {SettingIdentifier} : {Value}"; 
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