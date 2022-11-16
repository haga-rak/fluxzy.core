// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using System.Collections.Generic;

namespace Fluxzy.Archiving.Har
{
    public class HarModel
    {

    }

    public class HarModels
    {
        public string Version { get; set; } = "1.2";

        public HarCreator Creator { get; set; } =
            new HarCreator("fluxy", $"{FluxzyMetainformation.Version}", null); 

        public string ? Browser { get; set; } 
        
        public string ? Comment { get; set; }
        
    }

    public record HarCreator(string Name, string Version, string? Comment)
    {
        public string? Comment { get; set; } = Comment;
    }


    public class HarEntry
    {
        public DateTime StartDateTime { get; set; }

        public int Time { get; set; }

        public string?  ServerIpAddress { get; set; }

        public string ? Connection { get; set; }

        public string ? Comment { get; set; }
    }

    public class HarEntryRequest
    {
        public string Method { get; set; }

        public string Url { get; set; }

        public string HttpVersion { get; set; }

        public int HeaderSize { get; set; } = -1; 

        public long BodySize { get; set; } = -1; 

        public string ? Comment { get; set; }
    }

    public class HarCookie
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public string Path { get; set; }

        public string Domain { get; set; }

        public DateTime Expires { get; set; }

        public bool HttpOnly { get; set; }

        public bool Secure { get; set; }

        public string?  Comment { get; set; }
    }

    public class HarHeader
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public string  ? Comment { get; set; }
    }

    public class HarQueryString
    {

        public string Name { get; set; }

        public string Value { get; set; }

        public string? Comment { get; set; }
    }


    public class HarPostData
    {
        public string MimeType { get; set; }

        public List<HarParams> Params { get; set; } = new(); 

        public string Text { get; set; }

        public string? Comment { get; set; }
    }


    public class HarParams
    {

        public string Name { get; set; }

        public string Value { get; set; }

        public string FileName { get; set; }

        public string ContentType { get; set; }

        public string? Comment { get; set; }
    }

    public class HarContent
    {
        public long Size { get; set; }

        public int Compressing { get; set; }

        public string MimeType { get; set; }

        public string Text { get; set; }

        public string Encoding { get; set; }

        public string?  Comment { get; set; }
    }


    public class HarBeforeAfterRequest
    {
        public DateTime Expires { get; set; }

        public DateTime LastAccess { get; set; }

        public string ETag { get; set; }

        public long HitCount { get; set; }

        public string ? Comment { get; set; }
    }

    public class HarTimings
    {
        public int Blocked { get; set; }
        public int Dns { get; set; }
        public int Connect { get; set; }
        public int Send { get; set; }
        public int Wait { get; set; }
        public int Receive { get; set; }
        public int Ssl { get; set; }
        public string? Comment { get; set; }
    }

}