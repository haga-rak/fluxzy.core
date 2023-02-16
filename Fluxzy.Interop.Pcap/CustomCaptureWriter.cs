// Copyright © 2023 Haga RAKOTOHARIVELO

using SharpPcap;
using System.Runtime.InteropServices;
using SharpPcap.LibPcap;
using YamlDotNet.Core.Tokens;
using System.Buffers.Binary;

namespace Fluxzy.Interop.Pcap
{
    internal class CustomCaptureWriter : IDisposable
    {
        private readonly string _outFile;
        private readonly TimestampResolution _resolution;
        private readonly bool _isOsx;
        private readonly int _headerLength;
        private readonly decimal _unit;
        private readonly FileStream _fileStream;
        private readonly bool _isShortTimeVal;

        public CustomCaptureWriter(string outFile, TimestampResolution resolution)
        {
            _outFile = outFile;
            _resolution = resolution;
            _isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            _isShortTimeVal = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _headerLength = GetPreHeaderHeaderLength() + sizeof(long);
            _unit = _resolution == TimestampResolution.Nanosecond ? 1e9M : 1e6M;
            _fileStream = File.Open(_outFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        }

        public void Write(RawCapture rawCapture)
        {
            var data = rawCapture.Data;
            
            var timeValMicroseconds =  rawCapture.Timeval.MicroSeconds; 
            var timeValSeconds =  rawCapture.Timeval.Seconds;
            
            // Building header 

            Span<byte> headerBuffer = stackalloc byte[_headerLength];
            Span<byte> original = headerBuffer; 
            

            if (_isShortTimeVal)
            {
                BitConverter.TryWriteBytes(headerBuffer, (int) timeValSeconds);
                headerBuffer = headerBuffer.Slice(4); 
                BitConverter.TryWriteBytes(headerBuffer, (uint) timeValMicroseconds);
                headerBuffer = headerBuffer.Slice(4);
            }
            else
            {
                if (_isOsx)
                {
                    BitConverter.TryWriteBytes(headerBuffer, (long) timeValSeconds);
                    headerBuffer = headerBuffer.Slice(8);
                    BitConverter.TryWriteBytes(headerBuffer, (uint)timeValMicroseconds);
                    headerBuffer = headerBuffer.Slice(4);
                }
                else
                {
                    BitConverter.TryWriteBytes(headerBuffer, (long) timeValSeconds);
                    headerBuffer = headerBuffer.Slice(8);
                    BitConverter.TryWriteBytes(headerBuffer, (ulong) timeValMicroseconds);
                    headerBuffer = headerBuffer.Slice(8);
                }
            }

            BitConverter.TryWriteBytes(headerBuffer, (uint) data.Length);
            headerBuffer = headerBuffer.Slice(4);
            
            BitConverter.TryWriteBytes(headerBuffer, (uint) data.Length);

            _fileStream.Write(original);
            _fileStream.Write(data);
        }

        private int GetPreHeaderHeaderLength()
        {
            if (_isShortTimeVal)
                return 8;

            if (_isOsx)
                return 12;

            return 16; 
        }
        

        private void WriteHeader()
        {

        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }
    }
}
