// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using Fluxzy.Rules;
using YamlDotNet.Serialization;

namespace Fluxzy.Clients.Mock
{
    public class BodyContent
    {
        [JsonConstructor]
        public BodyContent(BodyContentLoadingType origin, BodyType type = default)
        {
            if (origin == 0) {
                origin = BodyContentLoadingType.FromString; 
            }

            Origin = origin;
            Type = type;
        }

        [JsonInclude]
        [PropertyDistinctive(Description = "Defines how the content body should be loaded",
            DefaultValue = "fromString")]
        public BodyContentLoadingType Origin { get; set; } = BodyContentLoadingType.FromString;

        [JsonInclude]
        [PropertyDistinctive(Description = "The body type. " +
                                           "Use this property to avoid defining manually `content-type` header." +
                                           "This property is ignored if `Content-Type` is defined explicitly.")]
        public BodyType Type { get; set; }
        

        [JsonInclude]
        [PropertyDistinctive(Description = "When Origin = fromString, the content text to be used as response body. Supports variable.")]
        public string? Text { get; set; }

        [JsonInclude]
        [PropertyDistinctive(Description = "When Origin = fromFile, the path to the file to be used as response body.")]
        public string? FileName { get; set; }

        [JsonIgnore]
        [YamlIgnore]
        public byte[]? Content { get; set; }

        [JsonInclude]
        [PropertyDistinctive(Description = "When Origin = fromImmediateArray, base64 encoded content of the response")]
        public string ? ContentBase64 {
            get
            {
                if (Content == null) {
                    return null;
                }

                return Convert.ToBase64String(Content);
            }
            set
            {
                if (value == null) {
                    Content = null;
                }
                else {
                    Content = Convert.FromBase64String(value);
                }
            }
        }
        
        public static BodyContent CreateFromFile(string fileName)
        {
            var result = new BodyContent(BodyContentLoadingType.FromFile) {
                FileName = fileName
            };

            return result;
        }

        public static BodyContent CreateFromArray(byte[] data)
        {
            var result = new BodyContent(BodyContentLoadingType.FromImmediateArray) {
                Content = data
            };

            return result;
        }

        public static BodyContent CreateFromString(string contentString)
        {
            var result = new BodyContent(BodyContentLoadingType.FromString) {
                Text = contentString
            };

            return result;
        }

        public string? GetContentTypeHeaderValue()
        {
            return Type.GetContentTypeHeaderValue();
        }

        public long GetLength()
        {
            switch (Origin) {
                case BodyContentLoadingType.FromString:
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

    [JsonConverter(typeof(JsonStringEnumConverter<BodyContentLoadingType>))]
    public enum BodyContentLoadingType
    {
        NotSet = 0,
        FromString = 1,
        FromImmediateArray,
        FromFile
    }
}
