// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.Mock
{
    public enum BodyType
    {
        Unknown = 0,
        Text = 1,
        Json,
        Xml,
        Html,
        Binary,
        Css,
        JavaScript = 7,
        Js = 7,
        Font = 8, 
        proto

    }
    
    public static class BodyTypeExtensions
    {
        public static string? GetContentTypeHeaderValue(this BodyType bodyType)
        {
            return bodyType switch
            {
                BodyType.Text => "text/plain; charset=utf-8",
                BodyType.Json => "application/json; charset=utf-8",
                BodyType.Xml => "application/xml; charset=utf-8",
                BodyType.Html => "text/html; charset=utf-8",
                BodyType.Binary => "application/octet-stream",
                BodyType.Css => "text/css; charset=utf-8",
                BodyType.JavaScript => "application/javascript; charset=utf-8",
                BodyType.Font => "application/font-woff2",
                BodyType.proto => "application/x-protobuf",
                _ => null
            };

        }
    }
}
