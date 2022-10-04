// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class MultipartFormContentProducer : IFormattingProducer<MultipartFormContentResult>
    {
        public string ResultTitle =>  "Multi-part content";

        public MultipartFormContentResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class MultipartFormContentResult : FormattingResult
    {
        public MultipartFormContentResult(string title) : base(title)
        {
        }
    }

    public class MultipartItem
    {
        public string RawHeader { get; set; }

        

        public string Name { get;  }

        public string ? ContentType { get;  }

        public string ? ContentDisposition { get;  }

        public long Offset { get;  }

        public long Length { get;  }
    }

    public class RawMultipartItem
    {
        public RawMultipartItem(string rawHeader, long offSet, long length)
        {
            RawHeader = rawHeader;
            OffSet = offSet;
            Length = length;
        }

        public string RawHeader { get; }

        public long OffSet { get;  }

        public long Length { get;  }
    }


    public static class MultipartReader
    {
        public static async Task<List<RawMultipartItem>> ReadItems(Stream stream, string boundary, int readBodyBufferSize = 1024 *8)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("Stream must be seekable", nameof(stream));

            var doubleCrLf = Encoding.ASCII.GetBytes("\r\n\r\n");
            var crlLf = Encoding.ASCII.GetBytes("\r\n");
            var endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary)!;

            var memoryStream = new MemoryStream();
            var tempBuffer = new byte[1024 * 8];
            List<RawMultipartItem> result = new List<RawMultipartItem>();

            long offsetStartRead = stream.Position;

            while (true)
            {
                int read = await stream.ReadAsync(tempBuffer, 0, tempBuffer.Length);

                if (read == 0)
                    break;  // EOF 
                

                memoryStream.Write(tempBuffer, 0, read);
                memoryStream.Flush();

                var internalBuffer = new Memory<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

                if (IndexOf(internalBuffer, doubleCrLf, out var index))
                {
                    // Header detected 
                    // Seek back to 

                    var rawHeaderData = internalBuffer.Slice(0, index + doubleCrLf.Length);
                    
                    var rawHeader = Encoding.UTF8.GetString(rawHeaderData.Span); 

                    // stream.Seek(-remain, SeekOrigin.Current);
                    var offSetBeforeReadingBody = offsetStartRead + rawHeaderData.Length;

                    stream.Seek(offSetBeforeReadingBody, SeekOrigin.Begin);
                    
                    // read body 
                    var bodyLength = await ReadBodyAsync(stream, endBoundaryBytes, readBodyBufferSize);
                    
                    if (bodyLength < 0)
                        throw new InvalidOperationException("Unexpected EOF");

                    result.Add(new RawMultipartItem(rawHeader,
                        offSetBeforeReadingBody,
                        bodyLength));

                    var offsetEndOfBody = offSetBeforeReadingBody + bodyLength + crlLf.Length;

                    stream.Seek(offsetEndOfBody, SeekOrigin.Begin);
                    offsetStartRead = offsetEndOfBody;


                    memoryStream = new MemoryStream();
                }
            }

            return result; 
        }


        private static async Task<long> ReadBodyAsync(Stream stream, ReadOnlyMemory<byte> endBoundary, int readBufferSize = 1024 *8)
        {
            var rawReadBuffer = new byte[readBufferSize];
            byte[] previousRawBuffer = new byte[rawReadBuffer.Length];
            Memory<byte> previousBuffer = default;

            int read;
            int totalRead = 0;

            long discarded = 0; 


            while ((read = await stream.ReadAtLeastAsync(rawReadBuffer, endBoundary.Length)) > 0)
            {
                totalRead += read;

                var readBuffer = new Memory<byte>(rawReadBuffer, 0, read);
                var readBufferText = Encoding.UTF8.GetString(readBuffer.Span);

                if (previousBuffer.Length == 0)
                {
                    // Check boundary only on read

                    var checkData = readBuffer;
                    var boundaryFound = 0; 

                    if ((boundaryFound = checkData.Span.IndexOf(endBoundary.Span)) >= 0)
                    {
                        var result = boundaryFound;

                        //var newOffset = result + endBoundary.Length;

                        //var seekValue = totalRead - newOffset;

                        //stream.Seek(-seekValue, SeekOrigin.Current);

                        return result;
                    }

                    Buffer.BlockCopy(rawReadBuffer, 0, previousRawBuffer, 0, read);
                    previousBuffer = new Memory<byte>(previousRawBuffer, 0, read);
                    
                }
                else
                {
                    byte[] sharedBufferRaw = new byte[previousBuffer.Length + read];

                    Memory<byte> checkData = sharedBufferRaw;

                    previousBuffer.CopyTo(checkData);
                    readBuffer.CopyTo(checkData.Slice(previousBuffer.Length));

                    var boundaryFound = 0;

                    if ((boundaryFound = checkData.Span.IndexOf(endBoundary.Span)) >= 0)
                    {
                        var result = discarded + boundaryFound;

                       // var newOffset = result + endBoundary.Length;

                        //var seekValue = totalRead - newOffset;

                        // stream.Seek(-seekValue, SeekOrigin.Current);

                        return result;
                    }

                    Buffer.BlockCopy(rawReadBuffer, 0, previousRawBuffer, 0, read);

                    discarded += previousBuffer.Length; 
                    previousBuffer = new Memory<byte>(previousRawBuffer, 0, read);

                }
            }

            return -1; 
        }



        private static bool IndexOf(ReadOnlyMemory<byte> data, ReadOnlySpan<byte> pattern, out int index)
        {
            return (index = data.Span.IndexOf(pattern)) >= 0;
        }
    }

}