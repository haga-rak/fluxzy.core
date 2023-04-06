// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Fluxzy.Clients;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.Streams;
using Fluxzy.Utils;
using Fluxzy.Writers;

namespace Fluxzy.Readers
{
    public class SazArchiveReader 
    {
        public bool IsSazArchive(string fileName)
        {
            try {
                using var zipArchive = ZipFile.Open(fileName, ZipArchiveMode.Read);
                var entry = zipArchive.GetEntry("raw/"); 

                return entry?.Length == 0; 
            }
            catch {
                // ignore zip reading error 
                return false; 
            }
        }

        public void WriteToDirectory(string fileName, string directory)
        {
            using var zipArchive = ZipFile.Open(fileName, ZipArchiveMode.Read);
            
            var directoryArchiveWriter = new DirectoryArchiveWriter(directory, null);

            var mainEntry = zipArchive.GetEntry("raw/");

            var textEntries = zipArchive.Entries
                      .Where(r => r.FullName.StartsWith("raw/") && r.Name.EndsWith("_c.txt"))
                      .ToList();
            
            // Read all connectionInfo 
        }

        private static Dictionary<string, ConnectionInfo> ReadConnectionInfo(
            IEnumerable<ZipArchiveEntry> textEntries, ZipArchive archive)
        {
            Dictionary<string, ConnectionInfo> connections = new(); 
            Dictionary<string, ExchangeInfo> exchanges = new();

            int connectionId = 1;
            int exchangeId = 1;

            foreach (var requestEntry in textEntries) {
                // Read file to \r\n

                if (!TryGetId(requestEntry.Name, out var id))
                    continue;  // No id no party 

                int doubleCrLf = -1;

                var xmlEntry = archive.Entries.FirstOrDefault(e => e.Name == $"{id}_m.xml");

                if (xmlEntry == null)
                    continue; // We ignore that entry

                XElement element;

                using (var tempStream = xmlEntry.Open()) {
                    element = XElement.Load(tempStream);
                }

                var requestStream = ReadHeaders(requestEntry, out var requestHeaders);

                if (requestStream == null)
                    continue; 

                var methodHeader = requestHeaders.FirstOrDefault(s => s.Name.Span.Equals(
                    Http11Constants.MethodVerb.Span, StringComparison.Ordinal));

                if (methodHeader.Value.Length == 0)
                    continue;

                var isConnect = methodHeader.Value.Span.Equals("CONNECT", StringComparison.OrdinalIgnoreCase);

                var authorityHeader = requestHeaders.FirstOrDefault(s => s.Name.Span.Equals(
                    Http11Constants.AuthorityVerb.Span
                    , StringComparison.Ordinal));
                
                if (authorityHeader.Name.Length == 0)
                    continue;

                var flatAuthority = authorityHeader.Value.ToString();

                if (!AuthorityUtility.TryParse(flatAuthority, out var host, out var port))
                    continue;
                
                var responseEntry = archive.Entries.FirstOrDefault(e => e.Name == $"{id}_s.txt");
                
                if (isConnect) {

                    var connectRequestBody = requestStream.ReadToEndGreedy();

                    if (string.IsNullOrWhiteSpace(connectRequestBody))
                        continue;

                    
                    // Read flat body 

                    // Parse TLS Version 
                    
                    var sslInfo = new SslInfo(
                        SazConnectBodyUtility.GetSslVersion(connectRequestBody) ?? SslProtocols.None,
                        



                        )



                    element.XPathSelectElement()


                    var connectionInfo = new ConnectionInfo(
                        connectionId ++,
                        new AuthorityInfo(host, port), 
                        new SslInfo(
                            
                            )
                    )
                }


                Http11Parser.Read()



            }

            return connections; 
        }

        private static Stream? ReadHeaders(ZipArchiveEntry requestEntry, out List<HeaderField> headers)
        {
            headers = new List<HeaderField>();

            int doubleCrLf;

            using (var tempStream = requestEntry.Open()) {
                doubleCrLf = ReadUntilDoubleCrlLf(tempStream);
            }

            if (doubleCrLf < 0)
                return null;

            using var stream = requestEntry.Open();

            var buffer = ArrayPool<byte>.Shared.Rent(doubleCrLf);
            string requestHeaderString;

            try {
                stream.ReadExact(buffer);

                if (!stream.ReadExact(buffer))
                    return null;

                requestHeaderString = Encoding.UTF8.GetString(buffer);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            headers = Http11Parser.Read(requestHeaderString.AsMemory(), true).ToList();

            stream.Drain(4);
            return stream;
        }

        private static int ReadUntilDoubleCrlLf(Stream stream)
        {
            var searchStream = new SearchStream(stream, Http11Constants.DoubleCrLf);
            searchStream.Drain(); 
            return (int) (searchStream.Result?.OffsetFound ?? -1);
        }

        private static string? GetConnectBodyString(Stream stream, int offset)
        {
            stream.DrainUntil(offset + 4); 

            using var streamReader = new StreamReader(stream);

            return streamReader.ReadToEnd();
        }

        private static bool TryGetId(string name, out int result)
        {
            result = -1;

            var endIndex = name.IndexOf('_');

            if (endIndex < 0)
                return false;

            return int.TryParse(name.Substring(0, endIndex), out result);
        }
    }

    public static class ZipUtility
    {

    }
}
