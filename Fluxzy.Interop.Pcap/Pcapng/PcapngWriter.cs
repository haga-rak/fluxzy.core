// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers;
using Fluxzy.Interop.Pcap.Writing;
using SharpPcap;

namespace Fluxzy.Interop.Pcap.Pcapng
{
    internal class PcapngWriter : IRawCaptureWriter
    {
        private readonly object _locker = new();

        private byte[]? _waitBuffer;
        private readonly PcapngStreamWriter _pcapngStreamWriter;

        private volatile Stream _workStream;
        private StreamWriter? _nssKeyLogStreamWriter = null;
        private string?  _nssKeyLogPath;

        public PcapngWriter(long key, string applicationName)
        {
            Key = key;

            _waitBuffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            _workStream = new MemoryStream(_waitBuffer);

            _pcapngStreamWriter = new PcapngStreamWriter(new PcapngGlobalInfo(applicationName));
            
            _pcapngStreamWriter.WriteSectionHeaderBlock(_workStream);
        }

        public bool Faulted { get; private set; }

        public void Flush()
        {
            lock (_pcapngStreamWriter)
            {
                if (_workStream is FileStream fileStream)
                    fileStream.Flush(); // We probably broke thread safety here 
            }
        }

        public void Register(string outFileName)
        {
            if (_waitBuffer == null)
                throw new InvalidOperationException("Already registered!");

            var fileInfo = new FileInfo(outFileName); 

            var path = fileInfo.Directory?.FullName;

            if (!string.IsNullOrWhiteSpace(path))
            {
                Directory.CreateDirectory(path);

                if (_nssKeyLogStreamWriter == null)
                {
                    _nssKeyLogPath =
                        Path.Combine(path, Path.GetFileNameWithoutExtension(fileInfo.FullName) + ".nsskeylog"); 
                }
            }

            var fileStream = File.Open(outFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            
            lock (_pcapngStreamWriter)
            {
                fileStream.Write(_waitBuffer, 0, (int) _workStream.Position); // We copy content to buffer 

                ArrayPool<byte>.Shared.Return(_waitBuffer);
                _waitBuffer = null;

                _workStream = fileStream;
            }
        }

        public void Write(PacketCapture packetCapture)
        {
            try
            {
                if (Faulted)
                    return;

                lock (_pcapngStreamWriter)
                    _pcapngStreamWriter.Write(_workStream, packetCapture);

            }
            catch (Exception)
            {
                Faulted = true;

                // Free the memory when disposed
                Dispose();

                throw;
            }
        }

        public void StoreKey(string nssKey)
        {
            try {
                if (_nssKeyLogStreamWriter == null && _nssKeyLogPath != null) {
                     _nssKeyLogStreamWriter = new StreamWriter(_nssKeyLogPath)
                    {
                        AutoFlush = true,
                        NewLine = "\r\n"
                    };
                }
                _nssKeyLogStreamWriter?.WriteLine($"{nssKey}");
            }
            catch {
                // This call comes from a different thread,
                // so we ignore if some nsskey is missing due to writer closed
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

                _workStream.Dispose();

                _nssKeyLogStreamWriter?.Dispose();
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