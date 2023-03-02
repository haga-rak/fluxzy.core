// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers;
using Fluxzy.Interop.Pcap.Pcapng;
using SharpPcap;

namespace Fluxzy.Interop.Pcap
{
    internal class PcapngWriter : IConnectionSubscription, IRawCaptureWriter
    {
        private readonly object _locker = new();
        
        private Stream _waitStream;
        private byte[]? _waitBuffer;
        private readonly PcapngStreamWriter _streamWriter;

        public PcapngWriter(long key, string applicationName)
        {
            Key = key;
            
            _waitBuffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            _waitStream = new MemoryStream(_waitBuffer);

            _streamWriter = new PcapngStreamWriter(new PcapngGlobalInfo(applicationName));

            // On écrit l'entête
            
            _streamWriter.WriteSectionHeaderBlock(_waitStream);
        }

        public bool Faulted { get; private set; }

        public void Flush()
        {
            if (_waitStream is FileStream fileStream)
                fileStream.Flush(); // We probably broke thread safety here 
        }

        public void Register(string outFileName)
        {
            if (_waitBuffer == null)
                throw new InvalidOperationException("Already registered!");

            var path = Path.GetDirectoryName(outFileName);

            if (!string.IsNullOrWhiteSpace(path))
                Directory.CreateDirectory(path);

            var fileStream = File.Open(outFileName, FileMode.Create, FileAccess.Write, FileShare.Read);

            fileStream.Write(PcapFileHeaderBuilder.Buffer);

            lock (_locker)
            {
                fileStream.Write(_waitBuffer, 0, (int)_waitStream.Position); // We copy content to buffer 

                ArrayPool<byte>.Shared.Return(_waitBuffer);
                _waitBuffer = null;

                _waitStream = fileStream;
            }
        }
        public void Write(PacketCapture packetCapture)
        {
            try
            {
                if (Faulted)
                    return;

                // We are dumping on file so no need to lock or whatsoever
                if (_waitStream is FileStream fileStream)
                {
                    _streamWriter.Write(fileStream, packetCapture);
                    return;
                }

                // Check for buffer overflow here 
                // _waitStream need to be protected 

                lock (_locker)
                    _streamWriter.Write(_waitStream, packetCapture);
            }
            catch (Exception ex)
            {
                Faulted = true;

                // Free the memory when disposed
                Dispose();

                throw;
            }
        }

        public void Dispose()
        {

            lock (_locker)
            {
                if (_waitBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_waitBuffer);
                    _waitBuffer = null;
                }

                _waitStream?.Dispose();

            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public long Key { get; }
    }
}