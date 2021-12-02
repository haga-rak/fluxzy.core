using System;
using System.Collections.Generic;
using Echoes.Encoding.Huffman;
using Echoes.Encoding.Utils;
using Echoes.Encoding.Utils.Interfaces;

namespace Echoes.Encoding.HPack
{
    public class  HPackDecoder : IDisposable
    {
        private readonly DecodingContext _decodingContext;
        private readonly PrimitiveOperation _primitiveOperation;
        private readonly CodecSetting _codecSetting;
        private readonly IMemoryProvider<char> _memoryProvider;
        private readonly Http11Parser _parser;
        
        private readonly List<HeaderField> _tempEntries = new List<HeaderField>();

        /// <summary>
        /// Decoding process 
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
            CodecSetting codecSetting = null, 
            IMemoryProvider<char> memoryProvider = null,
            Http11Parser parser = null,
            PrimitiveOperation primitiveOperation = null)
        {
            _decodingContext = decodingContext;
            _primitiveOperation = primitiveOperation ?? new PrimitiveOperation(new HuffmanCodec(HPackDictionary.Instance));
            _codecSetting = codecSetting ?? new CodecSetting();
            _memoryProvider = memoryProvider ?? new ArrayPoolMemoryProvider<char>();
            _parser = parser ?? new Http11Parser(_codecSetting.MaxHeaderLineLength, _memoryProvider);
        }

        public DecodingContext Context => _decodingContext;

        

        public ReadOnlySpan<char> Decode(ReadOnlySpan<byte> headerContent, Span<char> buffer, ref IList<HeaderField> originalFields)
        {
            _tempEntries.Clear();

            try
            {
                for (; ; )
                {
                    HeaderField tableEntry = ReadNextField(headerContent, out var readen);

                    if (readen <= 0)
                    {
                        break;
                    }

                    _tempEntries.Add(tableEntry);
                    originalFields.Add(tableEntry);

                    headerContent = headerContent.Slice(readen);
                }

                return _parser.Write(_tempEntries, buffer);
            }
            finally
            {
                _tempEntries.Clear();
            }
        }

        public ReadOnlySpan<char> Decode(ReadOnlySpan<byte> headerContent, Span<char> buffer)
        {
            _tempEntries.Clear();

            try
            {
                for (; ; )
                {
                    HeaderField tableEntry = ReadNextField(headerContent, out var readen);

                    if (readen <= 0)
                    {
                        break;
                    }

                    _tempEntries.Add(tableEntry);

                    headerContent = headerContent.Slice(readen);
                }

                return _parser.Write(_tempEntries, buffer);
            }
            finally
            {
                _tempEntries.Clear();
            }
        }
        
        private HeaderField ReadNextField(in ReadOnlySpan<byte> buffer, out int bytesReaden)
        {
            if (buffer.Length == 0)
            {
                bytesReaden = 0;
                return default; 
            }

            var type = ParsingMode.ParseType(buffer[0]);

            int index = -1;

            switch (type)
            {
                case HeaderFieldType.DynamicTableSizeUpdate:
                {
                    bytesReaden = _primitiveOperation.ReadInt32(buffer, 5, out var maxSize);
                    _decodingContext.UpdateMaxSize(maxSize);

                    return ReadNextField(buffer.Slice(0, bytesReaden), out bytesReaden);
                }
                case HeaderFieldType.IndexedHeaderField:
                {
                    bytesReaden = _primitiveOperation.ReadInt32(buffer, 7, out index);

                    if (!_decodingContext.TryGetEntry(index, out var tableEntry))
                        throw new HPackCodecException($"Referenced index header {index} is absent from decodingTable");

                    return tableEntry;
                }
                case HeaderFieldType.LiteralHeaderFieldIncrementalIndexingExistingName:
                {
                    var offsetLength = _primitiveOperation.ReadInt32(buffer, 6, out var headerIndex);

                    Span<char> lineBuffer = stackalloc char[_codecSetting.MaxHeaderLineLength];

                    // obtenir header value from static table

                    if (!_decodingContext
                            .TryGetEntry(headerIndex, out var header))
                    {
                        throw new HPackCodecException(
                            $"Requested headerIndex does not exist in static table {headerIndex}");
                    }

                    var headerValue = 
                        _primitiveOperation.ReadString(buffer.Slice(offsetLength), lineBuffer, out var headerValueLength);

                    bytesReaden = offsetLength + headerValueLength;

                    return _decodingContext.Register(header.Name.Span, headerValue);
                }
                case HeaderFieldType.LiteralHeaderFieldIncrementalIndexingWithName:
                {
                    Span<char> headerNameBuffer = stackalloc char[_codecSetting.MaxHeaderLineLength];
                    Span<char> headerValueBuffer = stackalloc char[_codecSetting.MaxHeaderLineLength];

                    var headerName = _primitiveOperation.ReadString(buffer.Slice(1), headerNameBuffer, out var offsetHeaderName);
                    var headerValue = _primitiveOperation.ReadString(buffer.Slice(1 + offsetHeaderName), headerValueBuffer, out var offsetHeaderValue);

                    bytesReaden = 1 + offsetHeaderName + offsetHeaderValue;

                    return _decodingContext.Register(headerName, headerValue);
                }
                case HeaderFieldType.LiteralHeaderFieldNeverIndexExistingName:
                case HeaderFieldType.LiteralHeaderFieldWithoutIndexingExistingName:
                {
                    var offsetLength = _primitiveOperation.ReadInt32(buffer, 4, out index);

                    if (!_decodingContext.TryGetEntry(index, out var tableEntry))
                    {
                        throw new HPackCodecException($"Referenced index header {index} is absent from decodingTable");
                    }

                    Span<char> lineBuffer = stackalloc char[_codecSetting.MaxHeaderLineLength];
                    var resultString = _primitiveOperation.ReadString(buffer.Slice(offsetLength), lineBuffer, out var offsetValueLength);
                        
                    bytesReaden = offsetLength + offsetValueLength;

                    return new HeaderField(tableEntry.Name.Span, resultString, _memoryProvider);
                }
                case HeaderFieldType.LiteralHeaderFieldNeverIndexWithName:
                case HeaderFieldType.LiteralHeaderFieldWithoutIndexingWithName:
                {
                    Span<char> headerNameBuffer = stackalloc char[_codecSetting.MaxHeaderLineLength];
                    Span<char> headerValueBuffer = stackalloc char[_codecSetting.MaxHeaderLineLength];

                    var headerName = _primitiveOperation.ReadString(buffer.Slice(1), headerNameBuffer, out var nameLength);
                    var headerValue = _primitiveOperation.ReadString(buffer.Slice(1 + nameLength), headerValueBuffer, out var valueLength);
                    bytesReaden = 1 + nameLength + valueLength;
                    return new HeaderField(headerName, headerValue, _memoryProvider);
                }
                default:
                    throw new HPackCodecException("Stream could not decoded");
            }
        }


        public void Dispose()
        {
            _memoryProvider?.Dispose();
        }

        public static HPackDecoder Create(CodecSetting codeSetting)
        {
            var memoryProvider = new ArrayPoolMemoryProvider<char>();

            return new HPackDecoder(new DecodingContext(memoryProvider), codeSetting, memoryProvider);
        }
    }
}