﻿// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.Text;
using Fluxzy.Extensions;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Fluxzy.Formatters
{
    public class ProducerContext : IDisposable
    {
        private byte[]? _internalBuffer;

        public bool IsTextContent { get; set; }

        public ExchangeInfo Exchange { get; }

        public IArchiveReader ArchiveReader { get; }

        public ProducerSettings Settings { get; }

        public long RequestBodyLength { get; }

        public ReadOnlyMemory<byte> RequestBody { get; }

        public ReadOnlyMemory<byte> ResponseBody { get; }

        public byte[]? ResponseBodyContent { get; }

        /// <summary>
        ///     If first 1024 utf8 chars are printable char, this property will contains
        ///     the decoded UTF8 text
        /// </summary>
        public string? RequestBodyText { get; }

        public CompressionInfo? CompressionInfo { get; }

        public string? ResponseBodyText { get; }

        public long? ResponseBodyLength { get; } = 0;

        public ProducerContext(
            ExchangeInfo exchange,
            IArchiveReader archiveReader,
            ProducerSettings settings)
        {
            Exchange = exchange;
            ArchiveReader = archiveReader;
            Settings = settings;

            using var requestBodyStream = archiveReader.GetRequestBody(exchange.Id);

            RequestBodyLength = requestBodyStream?.Length ?? 0;

            if (requestBodyStream != null && requestBodyStream.CanSeek && requestBodyStream.Length <
                Settings.MaxFormattableJsonLength)
            {
                _internalBuffer = ArrayPool<byte>.Shared.Rent((int)requestBodyStream.Length);
                var length = requestBodyStream.SeekableStreamToBytes(_internalBuffer);

                RequestBody = new ReadOnlyMemory<byte>(_internalBuffer, 0, length);

                if (ArrayTextUtilities.IsText(RequestBody.Span))
                    RequestBodyText = Encoding.UTF8.GetString(RequestBody.Span);
            }

            using var responseBodyStream = archiveReader.GetResponseBody(exchange.Id);

            if (responseBodyStream != null)
            {
                ResponseBodyLength = responseBodyStream.Length;

                ResponseBodyContent = CompressionHelper.ReadContent(exchange, responseBodyStream,
                    settings.MaximumRenderableBodyLength,
                    out var compressionInfo);

                var responseEncoding = exchange.GetResponseEncoding();

                if (ResponseBodyContent != null && (IsTextContent =
                        ArrayTextUtilities.IsText(ResponseBodyContent, 1024 * 1024, responseEncoding)))
                {
                    ResponseBody = ResponseBodyContent;
                    responseEncoding = responseEncoding ?? Encoding.UTF8;
                    ResponseBodyText = responseEncoding.GetString(ResponseBody.Span);
                }

                CompressionInfo = compressionInfo;
            }
        }

        public void Dispose()
        {
            if (_internalBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_internalBuffer);
                _internalBuffer = null;
            }
        }

        public ExchangeContextInfo GetContextInfo()
        {
            return new ExchangeContextInfo(ResponseBodyText, ResponseBodyLength, IsTextContent);
        }
    }

    public class ExchangeContextInfo
    {
        public string? ResponseBodyText { get; }

        public long? ResponseBodyLength { get; } = 0;

        public bool IsTextContent { get; set; }

        public ExchangeContextInfo(string? responseBodyText, long? responseBodyLength, bool isTextContent)
        {
            ResponseBodyText = responseBodyText;
            ResponseBodyLength = responseBodyLength;
            IsTextContent = isTextContent;
        }
    }
}
