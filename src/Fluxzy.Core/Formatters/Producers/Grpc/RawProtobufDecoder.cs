// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;

namespace Fluxzy.Formatters.Producers.Grpc
{
    internal static class RawProtobufDecoder
    {
        public static string Decode(ReadOnlySpan<byte> data, int maxLength = 2 * 1024 * 1024)
        {
            var sb = new StringBuilder();
            DecodeMessage(data, sb, 0, maxLength);
            return sb.ToString();
        }

        private static bool DecodeMessage(
            ReadOnlySpan<byte> data, StringBuilder sb, int indent, int maxLength)
        {
            var offset = 0;

            while (offset < data.Length) {
                if (sb.Length > maxLength) {
                    sb.AppendLine();
                    sb.Append(' ', indent);
                    sb.Append("... (output truncated)");
                    return false;
                }

                if (!TryReadVarint(data, ref offset, out var tag))
                    break;

                var fieldNumber = (int) (tag >> 3);
                var wireType = (int) (tag & 0x7);

                sb.Append(' ', indent);

                switch (wireType) {
                    case 0: // varint
                        if (!TryReadVarint(data, ref offset, out var varintValue))
                            return false;

                        sb.AppendLine($"field {fieldNumber} (varint): {varintValue}");
                        break;

                    case 1: // fixed64
                        if (offset + 8 > data.Length)
                            return false;

                        var fixed64 = BitConverter.ToUInt64(data.Slice(offset, 8));
                        offset += 8;
                        sb.AppendLine($"field {fieldNumber} (fixed64): {fixed64}");
                        break;

                    case 2: // length-delimited
                        if (!TryReadVarint(data, ref offset, out var length))
                            return false;

                        if (offset + (int) length > data.Length)
                            return false;

                        var fieldData = data.Slice(offset, (int) length);
                        offset += (int) length;

                        if (TryDecodeAsNestedMessage(fieldData, sb, indent, fieldNumber, maxLength)) {
                            // decoded as nested message
                        }
                        else if (IsLikelyUtf8String(fieldData)) {
                            var str = Encoding.UTF8.GetString(fieldData);
                            sb.AppendLine($"field {fieldNumber} (string): \"{EscapeString(str)}\"");
                        }
                        else {
                            sb.AppendLine(
                                $"field {fieldNumber} (bytes): [{FormatBytes(fieldData, 64)}]");
                        }

                        break;

                    case 5: // fixed32
                        if (offset + 4 > data.Length)
                            return false;

                        var fixed32 = BitConverter.ToUInt32(data.Slice(offset, 4));
                        offset += 4;
                        sb.AppendLine($"field {fieldNumber} (fixed32): {fixed32}");
                        break;

                    default:
                        sb.AppendLine($"field {fieldNumber} (unknown wire type {wireType})");
                        return false;
                }
            }

            return true;
        }

        private static bool TryDecodeAsNestedMessage(
            ReadOnlySpan<byte> data, StringBuilder sb, int indent, int fieldNumber, int maxLength)
        {
            if (data.Length == 0)
                return false;

            var testSb = new StringBuilder();

            if (!DecodeMessage(data, testSb, indent + 2, maxLength))
                return false;

            if (testSb.Length == 0)
                return false;

            sb.AppendLine($"field {fieldNumber} (embedded):");
            sb.Append(testSb);
            return true;
        }

        internal static bool TryReadVarint(ReadOnlySpan<byte> data, ref int offset, out ulong value)
        {
            value = 0;
            var shift = 0;

            while (offset < data.Length) {
                var b = data[offset++];
                value |= (ulong) (b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                    return true;

                shift += 7;

                if (shift >= 64)
                    return false;
            }

            return false;
        }

        private static bool IsLikelyUtf8String(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0)
                return false;

            for (var i = 0; i < data.Length; i++) {
                var b = data[i];

                // Allow printable ASCII, tab, newline, carriage return
                if (b >= 0x20 && b < 0x7F)
                    continue;

                if (b == 0x09 || b == 0x0A || b == 0x0D)
                    continue;

                // Allow valid UTF-8 multi-byte sequences
                if (b >= 0xC0 && b < 0xFE) {
                    var expectedContinuation = b < 0xE0 ? 1 : b < 0xF0 ? 2 : 3;

                    for (var j = 0; j < expectedContinuation; j++) {
                        if (i + 1 >= data.Length)
                            return false;

                        i++;

                        if ((data[i] & 0xC0) != 0x80)
                            return false;
                    }

                    continue;
                }

                return false;
            }

            return true;
        }

        private static string EscapeString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string FormatBytes(ReadOnlySpan<byte> data, int maxBytes)
        {
            var sb = new StringBuilder();
            var limit = Math.Min(data.Length, maxBytes);

            for (var i = 0; i < limit; i++) {
                if (i > 0)
                    sb.Append(' ');

                sb.Append(data[i].ToString("x2"));
            }

            if (data.Length > maxBytes)
                sb.Append($" ... ({data.Length} bytes total)");

            return sb.ToString();
        }
    }
}
