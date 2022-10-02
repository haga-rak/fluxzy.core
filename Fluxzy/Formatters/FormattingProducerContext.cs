// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters
{
    public class FormattingProducerContext : IDisposable
    {
        private byte[]?  _internalBuffer; 

        public FormattingProducerContext(
            ExchangeInfo exchange, 
            IArchiveReader archiveReader,
            ProducerSettings settings)
        {
            ArchiveReader = archiveReader;
            Settings = settings;
            
            using var requestBodyStream = archiveReader.GetRequestBody(exchange.Id);

            if (requestBodyStream != null && requestBodyStream.CanSeek && requestBodyStream.Length <
                Settings.MaxFormattableJsonLength)
            {

                _internalBuffer = ArrayPool<byte>.Shared.Rent((int) requestBodyStream.Length);
                int length = requestBodyStream.SeekableStreamToBytes(_internalBuffer);

                RequestBody = new ReadOnlyMemory<byte>(_internalBuffer, 0, length); 
            }
        }

        public IArchiveReader ArchiveReader { get; }

        public ProducerSettings Settings { get; }

        public ReadOnlyMemory<byte> RequestBody { get;  }

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