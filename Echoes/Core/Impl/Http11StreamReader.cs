using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Echoes.Core.Utils;

namespace Echoes.Core
{
    internal class Http11StreamReader : IHttpStreamReader
    {
        private static readonly int MaxHeaderSize = 8 * 1024;
        private static readonly int ReadBuffer = 32 * 1024;

        private readonly Stream _inStream;
        private readonly IReferenceClock _referenceClock;
        private readonly byte[] _remainingBuffer = new byte[MaxHeaderSize];
        private int _remainingBufferLength = -1;
        private readonly byte [] _readbodyBuffer = new byte[ReadBuffer];

        public Http11StreamReader(Stream inStream, IReferenceClock referenceClock)
        {
            _inStream = inStream;
            _referenceClock = referenceClock;
        }

        public unsafe Task<HeaderReadResult> ReadHeaderAsync(bool skipIfNoData)
        {
            return skipIfNoData ? ReadFromLocal() : ReadFromRemote();
        }

        private async Task<HeaderReadResult> ReadFromRemote()
        {
            byte[] headerBuffer = new byte[MaxHeaderSize];
            var result = new HeaderReadResult();
            ;

            int totalReaden = 0;
            _remainingBufferLength = 0;

            try
            {
                using (var headerMemoryStream = new MemoryStream(headerBuffer))
                {
                    int firstReaden;
                    var limitPosition = 0;

                    byte[] singleByte = new byte[1];

                    if ((firstReaden = await _inStream.ReadAsync(singleByte, 0, singleByte.Length).ConfigureAwait(false)) > 0)
                    {
                        // TODO Marqué début récéption octets ici
                        result.FirstByteReceived = _referenceClock.Instant();

                        totalReaden += firstReaden;
                        headerMemoryStream.Write(singleByte, 0, firstReaden);

                        byte[] buffer = new byte[640]; // This is a good buffer for header
                        int readen;

                        while ((readen = await _inStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                        {
                            totalReaden += readen;

                            if (totalReaden > headerBuffer.Length)
                                throw new InvalidOperationException("Header Too Large");

                            headerMemoryStream.Write(buffer, 0, readen);

                            var headerLength = FindNextDoubleCrLf(headerBuffer, totalReaden, ref limitPosition);

                            if (headerLength >= 0)
                            {
                                if (headerLength < totalReaden)
                                {
                                    _remainingBufferLength = totalReaden - headerLength;

                                    // There are a few response in the cable, let's back it up 
                                    using (var memoryStream = new MemoryStream(_remainingBuffer))
                                    {
                                        memoryStream.Write(headerBuffer, headerLength, totalReaden - headerLength);
                                    }
                                }

                                byte[] bufferResult = new byte[headerLength];
                                Buffer.BlockCopy(headerBuffer, 0, bufferResult, 0, bufferResult.Length);


                                result.LastByteReceived = _referenceClock.Instant();
                                result.Buffer = bufferResult;

                                return result;
                            }
                        }
                    }
                    else
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    throw new EchoesException("An error occured while trying to read header", ex);
                }

                throw;
            }

            return null;
        }

        private async Task<HeaderReadResult> ReadFromLocal()
        {
            byte [] headerBuffer = new byte[MaxHeaderSize];

            var result = new HeaderReadResult();

            int totalReaden = 0;
            _remainingBufferLength = 0;
            
            try
            {
                using (var headerMemoryStream = new MemoryStream(headerBuffer))
                {
                    var limitPosition = 0;

                    // TODO Marqué début récéption octets ici

                    byte[] buffer = new byte[1024]; // This is a good buffer for header
                    int readen;

                    while ((readen = await _inStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        if (result.FirstByteReceived == null)
                        {
                            result.FirstByteReceived = _referenceClock.Instant();
                        }

                        totalReaden += readen;

                        if (totalReaden > headerBuffer.Length)
                            throw new InvalidOperationException("Header Too Large");

                        headerMemoryStream.Write(buffer, 0, readen);

                        var headerLength = FindNextDoubleCrLf(headerBuffer, totalReaden, ref limitPosition);

                        if (headerLength >= 0)
                        {
                            if (headerLength < totalReaden)
                            {
                                _remainingBufferLength = totalReaden - headerLength;

                                // There are a few response ont the wire, let's back it up 
                                using (var memoryStream = new MemoryStream(_remainingBuffer))
                                {
                                    memoryStream.Write(headerBuffer, headerLength, totalReaden - headerLength);
                                }
                            }

                            byte [] bufferResult = new byte[headerLength];

                            Buffer.BlockCopy(headerBuffer, 0, bufferResult, 0, bufferResult.Length);

                            result.LastByteReceived = _referenceClock.Instant();
                            result.Buffer = bufferResult;

                            return result;
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    throw new EchoesException("An error occured while trying to read header", ex);
                }

                throw;
            }

            return null;
        }

        private static int FindNextDoubleCrLf(byte[] buffer, int bufferLength, ref int startSearchIndex)
        {
            // TODO Optimisation possible avec Span<byte>
            for (int i = Math.Max(0, startSearchIndex); i <= bufferLength - 4; i++)
            {
                var current = BitConverter.ToInt32(buffer, i);
                if (current == 0xA0D0A0D) // This match to \r\n\r\n
                    return i + 4;
            }

            startSearchIndex = bufferLength - 4;
            return -1;
        }

        public async Task<BodyReadResult> ReadBodyAsync(long length, params Stream[] outStreams)
        {
            if (length == 0)
                return BodyReadResult.CreateEmptyResult(_referenceClock);

            var result = new BodyReadResult();

            var actuallyReaden = await WriteRemaining(outStreams).ConfigureAwait(false);
            var firstCycle = true;

            while ((actuallyReaden < length))
            {
                var currentReadable = (int) Math.Min(_readbodyBuffer.Length, length - actuallyReaden);

                int currentReaden;

                try
                {
                    currentReaden = await _inStream.ReadAsync(_readbodyBuffer, 0, currentReadable).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is SocketException)
                    {
                        throw new EchoesException("An error occured while trying to read content body", ex);
                    }

                    throw;
                }
                finally
                {
                    if (firstCycle)
                    {
                        result.FirstByteReceived = _referenceClock.Instant();
                        firstCycle = false; 
                    }
                }

                if (currentReaden <= 0)
                    break; // EOF reached before length request 

                List<Task> writeTasks = new List<Task>();

                foreach (var outStream in outStreams)
                {
                    writeTasks.Add(outStream.WriteAsync(_readbodyBuffer, 0, currentReaden));
                }

                try
                {
                    await Task.WhenAll(writeTasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is SocketException)
                    {
                        throw new EchoesException("An error occured while trying to write content body", ex);
                    }

                    throw;
                }

                actuallyReaden += currentReaden;
            }

            if (length != actuallyReaden)
            {
            }

            //await Task.WhenAll(outStreams.Select(s => s.FlushAsync())).ConfigureAwait(false);

            result.FirstByteReceived = _referenceClock.Instant();
            result.Length = actuallyReaden; 

            return result;
        }

        private static async Task<long> InnerCopyBlock(Stream inStream, long length, params Stream[] outStreams)
        {
            if (length == 0)
                return 0;

            var buffer = new byte[ReadBuffer];

            var actuallyReaden = 0L;

            try
            {
                while ((actuallyReaden < length))
                {
                    var currentReadable = (int) Math.Min(buffer.Length, length - actuallyReaden);

                    int currentReaden = await inStream.ReadAsync(buffer, 0, currentReadable).ConfigureAwait(false);

                    if (currentReaden <= 0)
                        break; // EOF reached before length request 

                    List<Task> writeTasks = new List<Task>();

                    foreach (var outStream in outStreams)
                    {
                        writeTasks.Add(outStream.WriteAsync(buffer, 0, currentReaden));
                    }

                    try
                    {
                        await Task.WhenAll(writeTasks).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex is IOException || ex is SocketException)
                        {
                            throw new EchoesException("An error occured while trying to write content body", ex);
                        }

                        throw;
                    }

                    actuallyReaden += currentReaden;
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    throw new EchoesException("An error occured while trying to read content body", ex);
                }

                throw;
            }

            while ((actuallyReaden < length))
            {
                var currentReadable = (int) Math.Min(buffer.Length, length - actuallyReaden);

                int currentReaden = await inStream.ReadAsync(buffer, 0, currentReadable).ConfigureAwait(false);

                if (currentReaden <= 0)
                    break; // EOF reached before length request 

                List<Task> writeTasks = new List<Task>();

                foreach (var outStream in outStreams)
                {
                    writeTasks.Add(outStream.WriteAsync(buffer, 0, currentReaden));
                }

                try
                {
                    await Task.WhenAll(writeTasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is SocketException)
                    {
                        throw new EchoesException("An error occured while trying to write content body", ex);
                    }

                    throw;
                }

                actuallyReaden += currentReaden;
            }

            //try
            //{
            //    await Task.WhenAll(outStreams.Select(s => s.FlushAsync())).ConfigureAwait(false);
            //}
            //catch (Exception ex)
            //{
            //    if (ex is IOException || ex is SocketException)
            //    {
            //        throw new EchoesException("An error occured while trying to write content body", ex);
            //    }

            //    throw;
            //}


            return actuallyReaden;
        }

        public async Task<BodyReadResult> ReadBodyUntilEofAsync(params Stream[] outStreams)
        {
            var buffer = _readbodyBuffer;

            int readen;
            var result = new BodyReadResult();
            var firstCycle = true; 

            var actuallyReaden = await WriteRemaining(outStreams).ConfigureAwait(false);

            try
            {
                while ((readen = await _inStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    if (firstCycle)
                    {
                        result.FirstByteReceived = _referenceClock.Instant();
                        firstCycle = false; 
                    }

                    var blockReaden = readen;

                    try
                    {
                        await Task.WhenAll(outStreams.Select(o => o.WriteAsync(buffer, 0, blockReaden)))
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex is IOException || ex is SocketException)
                        {
                            throw new EchoesException("An error occured while trying to write content body", ex);
                        }

                        throw;
                    }


                    actuallyReaden += readen;
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    throw new EchoesException("An error occured while trying to read content body", ex);
                }

                throw;
            }

            result.LastByteReceived = _referenceClock.Instant();

            result.Length = actuallyReaden;
            return result;
        }

        /// <summary>
        /// This method is used to read the next CRLF line. This assumes ascii
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task<(string, byte[])> SlowReadToNextLine(Stream stream)
        {
            StringBuilder result = new StringBuilder();

            byte[] smallBuffer = new byte[1];

            using (var memoryStream = new MemoryStream())
            {
                while ((await stream.ReadAsync(smallBuffer, 0, smallBuffer.Length).ConfigureAwait(false)) > 0)
                {
                    memoryStream.Write(smallBuffer, 0, smallBuffer.Length);

                    char current = (char) smallBuffer[0];
                    result.Append(current);

                    if (result.Length < 2)
                    {
                        continue; 
                    }

                    var lastTowChar = result.ToString(result.Length -2, 2);

                    if (lastTowChar == "\r\n")
                    {
                        return (result.ToString().Replace("\r\n", string.Empty), memoryStream.ToArray());
                    }
                }

                return (result.ToString(), memoryStream.ToArray());
            }
        }

        public async Task<BodyReadResult> ReadBodyChunkedAsync(params Stream[] outStreams)
        {
            var totalReaden = 0L;
            var bufferLength = _remainingBufferLength < 0 ? 0 : _remainingBufferLength;
            var result = new BodyReadResult();
            var firstCycle = true; 
            
            using (var remainingBuffer = new MemoryStream(_remainingBuffer, 0, bufferLength))
            {
                using (var globalStream = new CombinedReadonlyStream(new[] { remainingBuffer, _inStream }))
                {
                    while (true)
                    {
                        (string, byte[]) nextValueData;

                        try
                        {
                            nextValueData = await SlowReadToNextLine(globalStream)
                                .ConfigureAwait(false);

                            if (firstCycle)
                            {
                                result.FirstByteReceived = _referenceClock.Instant();
                                firstCycle = false; 
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is IOException || ex is SocketException)
                            {
                                throw new EchoesException("An error occured while trying to read content body", ex);
                            }

                            throw;
                        }

                        totalReaden += nextValueData.Item2.Length;

                        try
                        {
                            var quantityToRead = long.Parse(nextValueData.Item1, System.Globalization.NumberStyles.HexNumber);

                            await Task.WhenAll(outStreams.Select(s => s.WriteAsync(nextValueData.Item2, 0, nextValueData.Item2.Length)))
                                .ConfigureAwait(false);

                            var totalWritten = await InnerCopyBlock(globalStream, quantityToRead + 2, outStreams)
                                .ConfigureAwait(false);

                            totalReaden += totalWritten;

                            if (totalWritten <= 2)
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is FormatException)
                            {
                                throw new EchoesException("Invalid chuncked size sent by remote", ex);
                            }

                            if (ex is IOException || ex is SocketException)
                            {
                                throw new EchoesException("An error occured while trying to write content body", ex);
                            }

                            throw;
                        }
                    }
                }
            }

            result.LastByteReceived = _referenceClock.Instant();
            result.Length = totalReaden;

            return result;

        }

        private async Task<long> WriteRemaining(Stream[] outStreams)
        {
            long actuallyReaden = 0L;

            try
            {
                if (_remainingBuffer != null && _remainingBufferLength > 0)
                {
                    await Task.WhenAll(outStreams.Select(s => s.WriteAsync(_remainingBuffer, 0, _remainingBufferLength)))
                        .ConfigureAwait(false);

                    //await Task.WhenAll(outStreams.Select(s => s.FlushAsync())).ConfigureAwait(false);

                    actuallyReaden += _remainingBufferLength;
                }

            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException)
                {
                    throw new EchoesException("An error occured while trying to write content body", ex);
                }

                throw;
            }

            return actuallyReaden;
        }
    }
}