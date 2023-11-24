// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Net;
using MessagePack;
using MessagePack.Formatters;

namespace Fluxzy.Archiving.MessagePack
{
    public class MessagePackAddressFormatter : IMessagePackFormatter<IPAddress>
    {
        public void Serialize(ref MessagePackWriter writer,
            IPAddress value, MessagePackSerializerOptions options)
        {
            Span<byte> data = stackalloc byte[32];

            if (!value.TryWriteBytes(data, out var written))
                throw new Exception("Failed to write bytes");

            var buffer = data.Slice(0, written);

            writer.WriteInt16((short)written);

            buffer.CopyTo(writer.GetSpan(written));
            writer.Advance(written);
        }

        public IPAddress Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var size = reader.ReadInt16();
            var buffer = reader.ReadRaw(size);

            Span<byte> data = stackalloc byte[32];

            buffer.CopyTo(data);

            return new IPAddress(data.Slice(0, size));
        }
    }
}
