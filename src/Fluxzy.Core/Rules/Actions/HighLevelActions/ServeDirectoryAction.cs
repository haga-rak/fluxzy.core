// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    [ActionMetadata("Serve a folder as a static web site. " +
                    "This action is made for mocking purpose and not production ready for a web site.")]
    public class ServeDirectoryAction : Action
    {
        public ServeDirectoryAction(string directory)
        {
            Directory = directory;
        }

        [ActionDistinctive(Description = "Directory to serve")]
        public string Directory { get; set;  }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Serve a folder as a static web site";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (!System.IO.Directory.Exists(Directory))
                throw new RuleExecutionFailureException($"Directory {Directory} does not exist");

            if (exchange == null)
                return default;

            if (!Uri.TryCreate(exchange.FullUrl, UriKind.Absolute, out var result)
                 || !result.Scheme.StartsWith("http")) {
                context.PreMadeResponse = new MockedResponseContent(404, new BodyContent(
                    BodyContentLoadingType.FromString, BodyType.Text
                ) {
                    Text = $"Cannot parse {exchange.FullUrl}"
                });
                return default; 
            }

            var fullDirectoryPath = new DirectoryInfo(Directory).FullName; 

            var path = result.LocalPath.Trim('/', '\\')
                             .Replace('\\', '/');

            var file = new FileInfo(Path.Combine(Directory, path));
            var dir = new DirectoryInfo(Path.Combine(Directory, path));

            if (file.FullName.Length < fullDirectoryPath.Length) {
                context.PreMadeResponse = new MockedResponseContent(404, new BodyContent(
                                       BodyContentLoadingType.FromString, BodyType.Text
                                                      )
                {
                    Text = $"Path not found {path}"
                });

                return default; 
            }

            if (File.Exists(file.FullName)) {
                var contentType = ContentTypeResolver.GetContentType(file.FullName); 

                context.PreMadeResponse = new MockedResponseContent(200, new BodyContent(
                                       BodyContentLoadingType.FromFile, BodyType.Binary) {
                    FileName = file.FullName
                })
                {
                    Headers = {
                        new ("Content-Type", contentType)
                    }
                };

                return default;
            }

            var indexFiles = new string[] { "index.html", "index.htm" };

            foreach (var indexFile in indexFiles) {
               var indexFullPath = new FileInfo(Path.Combine(dir.FullName, indexFile));
               if (indexFullPath.Exists) {
                    var contentType = ContentTypeResolver.GetContentType(indexFullPath.FullName);
                    context.PreMadeResponse = new MockedResponseContent(200, 
                        new BodyContent(BodyContentLoadingType.FromFile, BodyType.Binary)
                    {
                        FileName = indexFullPath.FullName
                    })
                    {
                        Headers = {
                            new ("Content-Type", contentType)
                        }
                    };
                    return default;
               }
            }

            context.PreMadeResponse = new MockedResponseContent(404, new BodyContent(
                                                      BodyContentLoadingType.FromString, BodyType.Text)
            {
                Text = $"Path not found {path}"
            });

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample("Serve a directory", 
                new ServeDirectoryAction("/path/to/my/static/website"));
        }
    }

    /// <summary>
    /// A small helper to get the content type of a file
    /// </summary>
    internal static class ContentTypeResolver
    {
        private static readonly Dictionary<string, string> MimeTypes = 
            new(StringComparer.OrdinalIgnoreCase)
        {
            {".txt", "text/plain"},
            {".html", "text/html"},
            {".htm", "text/html"},
            {".css", "text/css"},
            {".js", "application/javascript"},
            {".json", "application/json"},
            {".xml", "application/xml"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"},
            {".gif", "image/gif"},
            {".bmp", "image/bmp"},
            {".svg", "image/svg+xml"},
            {".tif", "image/tiff"},
            {".tiff", "image/tiff"},
            {".ico", "image/vnd.microsoft.icon"},
            {".webp", "image/webp"},
            {".mp4", "video/mp4"},
            {".mpeg", "video/mpeg"},
            {".mp3", "audio/mpeg"},
            {".wav", "audio/wav"},
            {".oga", "audio/ogg"},
            {".ogv", "video/ogg"},
            {".webm", "video/webm"},
            {".pdf", "application/pdf"},
            {".doc", "application/msword"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {".xls", "application/vnd.ms-excel"},
            {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {".ppt", "application/vnd.ms-powerpoint"},
            {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"}
        };

        public static string GetContentType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var extension = Path.GetExtension(fileName);

            if (MimeTypes.TryGetValue(extension, out var mimeType))
            {
                return mimeType;
            }

            return "application/octet-stream"; // Default for unknown types
        }
    }
}
