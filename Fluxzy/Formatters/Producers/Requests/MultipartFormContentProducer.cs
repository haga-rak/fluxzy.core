// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class MultipartFormContentProducer : IFormattingProducer<MultipartFormContentResult>
    {
        public string ResultTitle =>  "Multi-part content";

        public MultipartFormContentResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            var multipartHeader =
                exchangeInfo.GetRequestHeaders().FirstOrDefault(h =>
                    h.Name.Span.Equals("Content-type", StringComparison.OrdinalIgnoreCase)
                    && h.Value.Span.Contains("multipart", StringComparison.OrdinalIgnoreCase)
                    && h.Value.Span.Contains("boundary=", StringComparison.OrdinalIgnoreCase));

            if (multipartHeader == null)
                return null;

            var boundaryIndex = multipartHeader.Value.Span.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
            var boundary = multipartHeader.Value.Span.Slice(boundaryIndex + "boundary=".Length).ToString();

            using var stream = context.ArchiveReader.GetRequestBody(exchangeInfo.Id);

            if (stream == null)
                return null;

            var rawItems = MultipartReader.ReadItems(stream, boundary);

            var list = rawItems
                       .Select(s => s.BuildMultiPartItems())
                       .Where(t => t != null)
                       .Select(t => t!).ToList();

            if (!list.Any())
                return null;

            foreach (var item in list)
            {
                if (item.Length < context.Settings.MaxMultipartContentStringLength)
                {
                    var array = stream.GetSlicedStream(item.Offset, item.Length)
                                      .ToArrayGreedy();
                    
                    item.StringValue = ArrayTextUtilities.IsText(array) ? Encoding.UTF8.GetString(array) : string.Empty; 
                }
            }

            return new MultipartFormContentResult(ResultTitle, list);
        }
    }

    public class MultipartFormContentResult : FormattingResult
    {
        public MultipartFormContentResult(string title, List<MultipartItem> items) : base(title)
        {
            Items = items;
        }

        public List<MultipartItem> Items { get;  }
    }

    public class MultipartItem
    {
        public MultipartItem(string? name, string?  fileName, string? contentType, string? contentDisposition, long offset, long length)
        {
            Name = name;
            FileName = fileName;
            ContentType = contentType;
            ContentDisposition = contentDisposition;
            Offset = offset;
            Length = length;
        }

        public string? Name { get;  }
        public string? FileName { get; }

        public string ? ContentType { get;  }

        public string ? ContentDisposition { get;  }

        public long Offset { get;  }

        public long Length { get;  }

        public string?  RawHeader { get; set; }

        public string? StringValue { get; set; }
    }

    public class RawMultipartItem
    {
        public RawMultipartItem(string rawHeader, string boundary, long offSet, long length)
        {
            boundary = "--" + boundary;

            RawHeader = rawHeader
                .Replace("\r\n\r\n", "\r\n")
                .Replace(boundary, "")
                .TrimStart('\r', '\n');

            OffSet = offSet;
            Length = length;
        }

        public string RawHeader { get; }

        public long OffSet { get;  }

        public long Length { get;  }


        public MultipartItem?  BuildMultiPartItems()
        {
            var allLines = RawHeader.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var contentType =
                string.Join(":",
                    allLines.FirstOrDefault(r =>
                                r.StartsWith("Content-type", StringComparison.OrdinalIgnoreCase))
                            ?.Split(':')
                            .Skip(1) ?? Array.Empty<string>()).Trim();

            contentType = string.IsNullOrWhiteSpace(contentType) ? contentType : null; 

            var contentDispositionLine = allLines.FirstOrDefault(r =>
                r.StartsWith("Content-disposition", StringComparison.OrdinalIgnoreCase));

            var foundProperties = new Dictionary<string, string>();

            if (contentDispositionLine != null)
            {
                var result = Regex.Matches(contentDispositionLine,
                    @"([a-zA-Z]+)=""([^""]+)""");

                foreach (Match matchResult in result)
                {
                    if (matchResult.Success && matchResult.Groups.Count > 2)
                    {
                        foundProperties[matchResult.Groups[1].Value]
                            = matchResult.Groups[2].Value;
                    }
                }
            }

            foundProperties.TryGetValue("name", out var name);

            if (!foundProperties.TryGetValue("filename", out var fileName))
            {
                foundProperties.TryGetValue("file", out fileName);
            }

            if (string.IsNullOrWhiteSpace(name)
                && string.IsNullOrWhiteSpace(fileName))
            {
                return null; 
            }

            return new MultipartItem(name, fileName, contentType, "form-data", OffSet, Length)
            {
                RawHeader = RawHeader
            };
        }
    }


    public static class MultipartReader
    {
        public static Stream GetSlicedStream(this Stream seekableStream, long offsetBegin, long length)
        {
            seekableStream.Seek(offsetBegin, SeekOrigin.Begin);

            return new ContentBoundStream(seekableStream, length);
        }

        public static List<RawMultipartItem> ReadItems(Stream stream, string boundary, int readBodyBufferSize = 1024 *8)
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
                int read = stream.Read(tempBuffer, 0, tempBuffer.Length);

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
                    var bodyLength = ReadBody(stream, endBoundaryBytes, readBodyBufferSize);

                    if (bodyLength < 0)
                        return new List<RawMultipartItem>();

                    result.Add(new RawMultipartItem(rawHeader, boundary,
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

        private static long ReadBody(Stream stream, ReadOnlyMemory<byte> endBoundary, int readBufferSize = 1024 *8)
        {
            var rawReadBuffer = new byte[readBufferSize];
            byte[] previousRawBuffer = new byte[rawReadBuffer.Length];
            Memory<byte> previousBuffer = default;

            int read;
            int totalRead = 0;

            long discarded = 0; 


            while ((read = stream.ReadAtLeast(rawReadBuffer, endBoundary.Length)) > 0)
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