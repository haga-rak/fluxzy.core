// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Fluxzy.Clients.Mock
{
    public class BodyContent
    {

        [JsonConstructor]
        public BodyContent()
        {

        }

        public static BodyContent CreateFromFile(string fileName, string mimeType = null)
        {
            var result = new BodyContent()
            {
                LoadingType = BodyContentLoadingType.FromFile,
                FileName = fileName,
                MimeType = mimeType ?? "application/octet-stream"
            }; 

            return result;
        }

        public static BodyContent CreateFromArray(byte [] data, string mimeType = null)
        {
            var result = new BodyContent()
            {
                LoadingType = BodyContentLoadingType.FromImmediateArray,
                Content = data, 
                MimeType = mimeType ?? "application/octet-stream"
            }; 

            return result;
        }

        public static BodyContent CreateFromString(
            string contentString, string mimeType = null)
        {
            var result = new BodyContent()
            {
                LoadingType = BodyContentLoadingType.FromImmediateArray,
                Content = System.Text.Encoding.UTF8.GetBytes(contentString), 
                MimeType = mimeType ?? "text/plain; charset=utf-8" 
            }; 

            return result;
        }

        [JsonInclude]
        public BodyContentLoadingType LoadingType { get; private set; }

        [JsonInclude]
        public string MimeType { get; set; }

        [JsonInclude]
        public string FileName { get; private set; }

        [JsonInclude]
        public byte [] Content { get; private set; }

        public long GetLength()
        {

            switch (LoadingType)
            {
                case BodyContentLoadingType.FromImmediateArray:
                    return Content.Length; 
                case BodyContentLoadingType.FromFile:
                    return new FileInfo(FileName).Length; 
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Stream GetStream()
        {
            switch (LoadingType)
            {
                case BodyContentLoadingType.FromImmediateArray:
                    return new MemoryStream(Content); 
                case BodyContentLoadingType.FromFile:
                    return File.OpenRead(FileName); 
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