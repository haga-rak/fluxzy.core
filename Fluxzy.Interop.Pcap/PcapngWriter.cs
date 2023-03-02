// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Buffers;
using System.IO;
using System.Threading.Channels;
using Fluxzy.Interop.Pcap.Pcapng;
using Fluxzy.Misc;
using SharpPcap;

namespace Fluxzy.Interop.Pcap
{
    internal class PcapngWriter : IConnectionSubscription, IRawCaptureWriter
    {
        private readonly object _locker = new();

        private readonly Channel<string> _nssKeyChannels = Channel.CreateUnbounded<string>();

        private Stream _workStream;
        private byte[]? _waitBuffer;
        private readonly PcapngStreamWriter _pcapngStreamWriter;

        public PcapngWriter(long key, string applicationName)
        {
            Key = key;
            
            _waitBuffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            _workStream = new MemoryStream(_waitBuffer);

            _pcapngStreamWriter = new PcapngStreamWriter(new PcapngGlobalInfo(applicationName));

            // On écrit l'entête
            
            _pcapngStreamWriter.WriteSectionHeaderBlock(_workStream);
        }

        public bool Faulted { get; private set; }

        public void Flush()
        {
            if (_workStream is FileStream fileStream)
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

            // fileStream.Write(PcapFileHeaderBuilder.Buffer);

            lock (_locker)
            {
                fileStream.Write(_waitBuffer, 0, (int)_workStream.Position); // We copy content to buffer 

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
                
                // We are dumping on file so no need to lock or whatsoever

                if (_workStream is FileStream fileStream)
                {
                    if (_nssKeyChannels.Reader.TryPeek(out _))
                    {
                        var pendingChannelkeys = new List<string>();
                        var readAll = _nssKeyChannels.Reader.TryReadAll(pendingChannelkeys);

                        //_pcapngStreamWriter.WriteNssKey(fileStream, string.Join("\r\n", readAll));
                    }
                    
                    _pcapngStreamWriter.Write(fileStream, packetCapture);
                    return;
                }

                // Check for buffer overflow here 
                // _waitStream need to be protected 

                lock (_locker)
                {
                    if (_nssKeyChannels.Reader.TryPeek(out _))
                    {
                        var pendingChannelkeys = new List<string>();
                        var readAll = _nssKeyChannels.Reader.TryReadAll(pendingChannelkeys);
                       // _pcapngStreamWriter.WriteNssKey(_workStream, string.Join("\r\n", readAll));
                    }

                    _pcapngStreamWriter.Write(_workStream, packetCapture);
                }
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
           // _nssKeyChannels.Writer.TryWrite(nssKey);
            _pcapngStreamWriter.WriteNssKey(_workStream, nssKey + "\r\n");
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

                _workStream?.Dispose();

                _nssKeyChannels.Writer.TryComplete();

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