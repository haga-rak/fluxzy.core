// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace Fluxzy.Clients.Mock
{
    public class BodyContent
    {
        [JsonConstructor]
        public BodyContent(BodyContentLoadingType origin, string? mime)
        {
            Origin = origin;
            Mime = mime;
        }

        [JsonInclude]
        public BodyContentLoadingType Origin { get; set; }

        [JsonInclude]
        public string? Mime { get; set;  }

        [JsonInclude]
        public string ? Text { get; set; }

        [JsonInclude]
        public string? FileName { get; private set; }

        [JsonInclude]
        public byte[]? Content { get; private set; }

        public static BodyContent CreateFromFile(string fileName, string? mimeType = null)
        {
            var result = new BodyContent(BodyContentLoadingType.FromFile, mimeType ?? "application/octet-stream") {
                FileName = fileName
            };

            return result;
        }

        public static BodyContent CreateFromArray(byte[] data, string? mimeType = null)
        {
            var result = new BodyContent(BodyContentLoadingType.FromImmediateArray,
                mimeType ?? "application/octet-stream") {
                Content = data
            };

            return result;
        }

        public static BodyContent CreateFromString(string contentString, string? mimeType = null)
        {
            var result = new BodyContent(BodyContentLoadingType.FromImmediateArray,
                mimeType ?? "text/plain; charset=utf-8") {
                Content = Encoding.UTF8.GetBytes(contentString)
            };

            return result;
        }

        public long GetLength()
        {
            switch (Origin) {
                case BodyContentLoadingType.FromString :
                    return Text == null ? 0 : Encoding.UTF8.GetByteCount(Text);

                case BodyContentLoadingType.FromImmediateArray:
                    return Content!.Length;

                case BodyContentLoadingType.FromFile:
                    return FileName == null ? 0 : new FileInfo(FileName).Length;

                default:
                    return -1; 
            }
        }

        public Stream GetStream()
        {
            switch (Origin) {
                case BodyContentLoadingType.FromImmediateArray:
                    return new MemoryStream(Content!);

                case BodyContentLoadingType.FromFile:
                    return File.OpenRead(FileName!);
                
                case BodyContentLoadingType.FromString:
                    return new MemoryStream(Encoding.UTF8.GetBytes(Text!));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum BodyContentLoadingType
    {
        FromString = 1, 
        FromImmediateArray ,
        FromFile
    }
}
