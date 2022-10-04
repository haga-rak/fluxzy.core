// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.Text;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters
{
    public class ProducerContext : IDisposable
    {
        private byte[]?  _internalBuffer; 

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
                _internalBuffer = ArrayPool<byte>.Shared.Rent((int) requestBodyStream.Length);
                int length = requestBodyStream.SeekableStreamToBytes(_internalBuffer);

                RequestBody = new ReadOnlyMemory<byte>(_internalBuffer, 0, length);

                if (ArrayTextUtilities.IsText(RequestBody.Span))
                {
                    RequestBodyText = Encoding.UTF8.GetString(RequestBody.Span);
                }
            }
        }

        public ExchangeInfo Exchange { get; }

        public IArchiveReader ArchiveReader { get; }

        public ProducerSettings Settings { get; }

        public long RequestBodyLength { get; } = 0;


        public ReadOnlyMemory<byte> RequestBody { get;  }

        /// <summary>
        /// If first 1024 utf8 chars are printable char, this property will contains
        /// the decoded UTF8 text
        /// </summary>
        public string ? RequestBodyText { get;  }

        public void Dispose()
        {
            if (_internalBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_internalBuffer);
                _internalBuffer = null; 
            }
        }
    }
}