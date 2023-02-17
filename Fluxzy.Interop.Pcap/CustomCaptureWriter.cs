// Copyright © 2023 Haga RAKOTOHARIVELO

using System.Buffers;
using SharpPcap;
using System.Runtime.InteropServices;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    internal class CustomCaptureWriter : IDisposable, IConnectionSubscription
    {
        private readonly object _locker = new object();
        
        private readonly bool _isOsx;
        private readonly int _headerLength;
        private readonly bool _isShortTimeVal;
        
        private  Stream _waitStream;
        private  byte[]? _waitBuffer;

        public CustomCaptureWriter(long key)
        {
            Key = key;
            _isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            _isShortTimeVal = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _headerLength = GetPreHeaderHeaderLength() + sizeof(long);

            // TODO put _waitBuffer length into config file / env variable
            
            _waitBuffer = ArrayPool<byte>.Shared.Rent(65536); 
            _waitStream = new MemoryStream(_waitBuffer);
        }

        public bool Registered => _waitStream is FileStream; 

        public bool Faulted { get; private set; }

        public void Flush()
        {
            _waitStream.Flush();
        }

        public void Register(string outFileName)
        {
            if (_waitBuffer == null)
                throw new InvalidOperationException("Already registered!"); 
            
            var fileStream = File.Open(outFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            fileStream.Write(PcapFileHeaderBuilder.Buffer);

            lock (_locker) {
                fileStream.Write(_waitBuffer, 0, (int) _waitStream.Position); // We copy content to buffer 
                
                ArrayPool<byte>.Shared.Return(_waitBuffer);
                _waitBuffer = null; 
                
                _waitStream = fileStream;
            }
        }
        

        public void Write(ReadOnlySpan<byte> data, PosixTimeval timeVal)
        {
            try {
                
                // We are dumping on file so no need to lock or whatsoever
                if (_waitStream is FileStream fileStream)
                {
                    InternalWrite(data, timeVal, fileStream);
                    return;
                }


                // Check for buffer overflow here 
                // waitstream need to be protected 
                lock (_locker)
                    InternalWrite(data, timeVal, _waitStream);
            }
            catch {
                Faulted = true;

                throw; 
            }
        }

        private void InternalWrite(ReadOnlySpan<byte> data, PosixTimeval timeVal, Stream destination)
        {
            var timeValMicroseconds = timeVal.MicroSeconds;
            var timeValSeconds = timeVal.Seconds;

            // Building header 

            Span<byte> headerBuffer = stackalloc byte[_headerLength];
            Span<byte> original = headerBuffer;


            if (_isShortTimeVal) {
                BitConverter.TryWriteBytes(headerBuffer, (int) timeValSeconds);
                headerBuffer = headerBuffer.Slice(4);
                BitConverter.TryWriteBytes(headerBuffer, (uint) timeValMicroseconds);
                headerBuffer = headerBuffer.Slice(4);
            }
            else {
                if (_isOsx) {
                    BitConverter.TryWriteBytes(headerBuffer, (long) timeValSeconds);
                    headerBuffer = headerBuffer.Slice(8);
                    BitConverter.TryWriteBytes(headerBuffer, (uint) timeValMicroseconds);
                    headerBuffer = headerBuffer.Slice(4);
                }
                else {
                    BitConverter.TryWriteBytes(headerBuffer, (long) timeValSeconds);
                    headerBuffer = headerBuffer.Slice(8);
                    BitConverter.TryWriteBytes(headerBuffer, (ulong) timeValMicroseconds);
                    headerBuffer = headerBuffer.Slice(8);
                }
            }

            BitConverter.TryWriteBytes(headerBuffer, (uint) data.Length);
            headerBuffer = headerBuffer.Slice(4);

            BitConverter.TryWriteBytes(headerBuffer, (uint) data.Length);

         
            destination.Write(original);
            destination.Write(data);
        }
        
        private int GetPreHeaderHeaderLength()
        {
            if (_isShortTimeVal)
                return 8;

            if (_isOsx)
                return 12;

            return 16; 
        }

        public void Dispose()
        {

            lock (_locker)
            {
                if (_waitBuffer != null) {
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


    internal static class PcapFileHeaderBuilder
    {
        public static byte[] Buffer { get;  }

        static PcapFileHeaderBuilder()
        {
            var tempFile = Environment.GetEnvironmentVariable("FLUXZY_BASE_DIR")
                           ?? "%appdata%/fluxzy/pcap/header";

            Directory.CreateDirectory(tempFile);

            var tempFileName = Path.Combine(tempFile, "header.pcap");

            using (var deviceWriter = new CaptureFileWriterDevice(tempFileName, FileMode.Create))
            {
                // Writing file header
                deviceWriter.Open();
            }

            Buffer = File.ReadAllBytes(tempFileName);
        }
    }
}
