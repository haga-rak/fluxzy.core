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
        public BodyContent(BodyContentLoadingType loadingType, string mimeType)
        {
            LoadingType = loadingType;
            MimeType = mimeType;
        }

        public BodyContentLoadingType LoadingType { get; set; }

        public string MimeType { get; }

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
            switch (LoadingType) {
                case BodyContentLoadingType.FromImmediateArray:
                    return Content!.Length;

                case BodyContentLoadingType.FromFile:
                    return new FileInfo(FileName).Length;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Stream GetStream()
        {
            switch (LoadingType) {
                case BodyContentLoadingType.FromImmediateArray:
                    return new MemoryStream(Content!);

                case BodyContentLoadingType.FromFile:
                    return File.OpenRead(FileName!);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum BodyContentLoadingType
    {
        FromImmediateArray = 1,
        FromFile
    }
}
