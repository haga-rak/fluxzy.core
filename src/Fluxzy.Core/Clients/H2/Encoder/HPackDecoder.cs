// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Clients.H2.Encoder.HPack;
using Fluxzy.Clients.H2.Encoder.Huffman;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;

namespace Fluxzy.Clients.H2.Encoder
{
    public class HPackDecoder : IDisposable
    {
        private readonly CodecSetting _codecSetting;
        private readonly ArrayPoolMemoryProvider<char> _memoryProvider;
        private readonly PrimitiveOperation _primitiveOperation;

        private readonly List<HeaderField> _tempEntries = new();

        /// <summary>
        ///     Decoding process
        /// </summary>
        /// <param name="decodingContext"></param>
        /// <param name="primitiveOperation"></param>
        /// <param name="codecSetting"></param>
        /// <param name="memoryProvider"></param>
        /// <param name="parser"></param>
        /// <exception cref="HPackCodecException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal HPackDecoder(
            DecodingContext decodingContext,
            CodecSetting? codecSetting = null,
            ArrayPoolMemoryProvider<char>? memoryProvider = null,
            PrimitiveOperation? primitiveOperation = null)
        {
            Context = decodingContext;

            _primitiveOperation =
                primitiveOperation ?? new PrimitiveOperation(new HuffmanCodec(HPackDictionary.Instance));

            _codecSetting = codecSetting ?? new CodecSetting();
            _memoryProvider = memoryProvider ?? ArrayPoolMemoryProvider<char>.Default;
        }

        public DecodingContext Context { get; }

        public void Dispose()
        {
        }
        public ReadOnlySpan<char> Decode(ReadOnlySpan<byte> headerContent, Span<char> buffer)
        {
            _tempEntries.Clear();

            try {
                for (;;) {
                    var tableEntry = ReadNextField(headerContent, out var readen);

                    if (readen <= 0)
                        break;

                    // this leads to unboxing which is very costly
                    // to meditate

                    _tempEntries.Add(tableEntry);

                    headerContent = headerContent.Slice(readen);
                }

                return Http11Parser.Write(_tempEntries, buffer);
            }
            finally {
                _tempEntries.Clear();
            }
        }

        /// <summary>
        ///     Decode HPACK-encoded bytes into raw header fields without HTTP/1.1 conversion.
        ///     Used for HTTP trailers which have no pseudo-headers.
        /// </summary>
        public List<HeaderField> DecodeTrailerFields(ReadOnlySpan<byte> headerContent)
        {
            var result = new List<HeaderField>();

            while (headerContent.Length > 0) {
                var tableEntry = ReadNextField(headerContent, out var bytesRead);

                if (bytesRead <= 0)
                    break;

                result.Add(tableEntry);
                headerContent = headerContent.Slice(bytesRead);
            }

            return result;
        }

        private HeaderField ReadNextField(in ReadOnlySpan<byte> buffer, out int bytesReaden)
        {
            if (buffer.Length == 0) {
                bytesReaden = 0;

                return default;
            }

            var type = ParsingMode.ParseType(buffer[0]);

            var index = -1;

            switch (type) {
                case HeaderFieldType.DynamicTableSizeUpdate: {
                    var currentByteReaden = _primitiveOperation.ReadInt32(buffer, 5, out var maxSize);
                    Context.UpdateMaxSize(maxSize);

                    var res = ReadNextField(buffer.Slice(currentByteReaden), out var nextRead);
                    bytesReaden = currentByteReaden + nextRead;

                    return res;
                }

                case HeaderFieldType.IndexedHeaderField: {
                    bytesReaden = _primitiveOperation.ReadInt32(buffer, 7, out index);

                    if (!Context.TryGetEntry(index, out var tableEntry))
                        throw new HPackCodecException($"Referenced index header {index} is absent from decodingTable");

                    return tableEntry;
                }

                case HeaderFieldType.LiteralHeaderFieldIncrementalIndexingExistingName: {
                    var offsetLength = _primitiveOperation.ReadInt32(buffer, 6, out var headerIndex);

                    // obtenir header value from static table

                    if (!Context
                            .TryGetEntry(headerIndex, out var header)) {
                        throw new HPackCodecException(
                            $"Requested headerIndex does not exist in static table {headerIndex}");
                    }

                    var valPrefix = buffer.Slice(offsetLength);
                    _primitiveOperation.ReadStringPrefix(valPrefix, out var valWireLen, out var valIsHuffman);
                    var valCharBudget = valIsHuffman ? valWireLen * 2 : valWireLen;

                    var lineBuffer =
                        valCharBudget <= _codecSetting.MaxStackAllocationLength
                            ? stackalloc char[valCharBudget]
                            : new char[valCharBudget];

                    var headerValue =
                        _primitiveOperation
                            .ReadString(valPrefix, lineBuffer, out var headerValueLength);

                    bytesReaden = offsetLength + headerValueLength;

                    return Context.Register(header.Name.Span, headerValue);
                }

                case HeaderFieldType.LiteralHeaderFieldIncrementalIndexingWithName: {
                    var nameSlice = buffer.Slice(1);
                    _primitiveOperation.ReadStringPrefix(nameSlice, out var nameWireLen, out var nameIsHuffman);
                    var nameCharBudget = nameIsHuffman ? nameWireLen * 2 : nameWireLen;

                    var headerNameBuffer =
                        nameCharBudget <= _codecSetting.MaxStackAllocationLength
                            ? stackalloc char[nameCharBudget]
                            : new char[nameCharBudget];

                    var headerName =
                        _primitiveOperation.ReadString(nameSlice, headerNameBuffer, out var offsetHeaderName);

                    var valSlice = buffer.Slice(1 + offsetHeaderName);
                    _primitiveOperation.ReadStringPrefix(valSlice, out var valWireLen2, out var valIsHuffman2);
                    var valCharBudget2 = valIsHuffman2 ? valWireLen2 * 2 : valWireLen2;

                    var headerValueBuffer = valCharBudget2 <= _codecSetting.MaxStackAllocationLength
                        ? stackalloc char[valCharBudget2]
                        : new char[valCharBudget2];

                    var headerValue = _primitiveOperation.ReadString(valSlice,
                        headerValueBuffer, out var offsetHeaderValue);

                    bytesReaden = 1 + offsetHeaderName + offsetHeaderValue;

                    return Context.Register(headerName, headerValue);
                }

                case HeaderFieldType.LiteralHeaderFieldNeverIndexExistingName:
                case HeaderFieldType.LiteralHeaderFieldWithoutIndexingExistingName: {
                    var offsetLength = _primitiveOperation.ReadInt32(buffer, 4, out index);

                    if (!Context.TryGetEntry(index, out var tableEntry))
                        throw new HPackCodecException($"Referenced index header {index} is absent from decodingTable");

                    var valSlice3 = buffer.Slice(offsetLength);
                    _primitiveOperation.ReadStringPrefix(valSlice3, out var valWireLen3, out var valIsHuffman3);
                    var valCharBudget3 = valIsHuffman3 ? valWireLen3 * 2 : valWireLen3;

                    var lineBuffer =
                        valCharBudget3 <= _codecSetting.MaxStackAllocationLength
                            ? stackalloc char[valCharBudget3]
                            : new char[valCharBudget3];

                    var resultString = _primitiveOperation.ReadString(valSlice3, lineBuffer,
                        out var offsetValueLength);

                    bytesReaden = offsetLength + offsetValueLength;

                    return new HeaderField(tableEntry.Name.Span, resultString, _memoryProvider);
                }

                case HeaderFieldType.LiteralHeaderFieldNeverIndexWithName:
                case HeaderFieldType.LiteralHeaderFieldWithoutIndexingWithName: {
                    var nameSlice4 = buffer.Slice(1);
                    _primitiveOperation.ReadStringPrefix(nameSlice4, out var nameWireLen4, out var nameIsHuffman4);
                    var nameCharBudget4 = nameIsHuffman4 ? nameWireLen4 * 2 : nameWireLen4;

                    var headerNameBuffer =
                        nameCharBudget4 <= _codecSetting.MaxStackAllocationLength
                            ? stackalloc char[nameCharBudget4]
                            : new char[nameCharBudget4];

                    var headerName =
                        _primitiveOperation.ReadString(nameSlice4, headerNameBuffer, out var nameLength);

                    var valSlice4 = buffer.Slice(1 + nameLength);
                    _primitiveOperation.ReadStringPrefix(valSlice4, out var valWireLen4, out var valIsHuffman4);
                    var valCharBudget4 = valIsHuffman4 ? valWireLen4 * 2 : valWireLen4;

                    var headerValueBuffer =
                        valCharBudget4 <= _codecSetting.MaxStackAllocationLength
                            ? stackalloc char[valCharBudget4]
                            : new char[valCharBudget4];

                    var headerValue = _primitiveOperation.ReadString(valSlice4, headerValueBuffer,
                        out var valueLength);

                    bytesReaden = 1 + nameLength + valueLength;

                    return new HeaderField(headerName, headerValue, _memoryProvider);
                }

                default:
                    throw new HPackCodecException("Stream could not decoded");
            }
        }

        public static HPackDecoder Create(CodecSetting codeSetting, Authority authority)
        {
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;

            return new HPackDecoder(new DecodingContext(authority, memoryProvider), codeSetting, memoryProvider);
        }
    }
}
