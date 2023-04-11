// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using Fluxzy.Har;
using Fluxzy.Utils;
using Fluxzy.Writers;

namespace Fluxzy.Readers
{
    public class HarImportEngine : IImportEngine
    {
        public bool IsFormat(string fileName)
        {
            return fileName.EndsWith(".har", StringComparison.OrdinalIgnoreCase);
        }

        public void WriteToDirectory(string fileName, string directory)
        {
            var directoryArchiveWriter = new DirectoryArchiveWriter(directory, null);
            directoryArchiveWriter.Init();

            using var inStream = File.OpenRead(fileName);

            var model = System.Text.Json.JsonSerializer.Deserialize<HarReadModel>(inStream)!;

            Dictionary<string, ConnectionInfo> internalConnections = new();
            int onTheFlyId = 1;
            int connectionId = 1; 


            foreach (var entry in model.Entries) {

                if (entry.Request == null)
                    continue;

                if (entry.Response == null)
                    continue;

                if (!ExchangeUtility.TryGetAuthorityInfo(entry.Request.FullUrl,
                        out var hostName, out var port, out var secure)) {
                    continue; 
                }

                var received = entry.StartDateTime;
                var poolReceived = received.AddMilliseconds(entry.Timings?.Blocked ?? 0); 
                var dnsStart = poolReceived;
                var dnsEnd = poolReceived.AddMilliseconds(entry?.Timings?.Dns ?? 0);
                var connectStart = dnsEnd;
                var connectEnd = connectStart.AddMilliseconds(entry?.Timings?.Connect ?? 0);
                var sslStart = connectStart; 
                var sslEnd = sslStart.AddMilliseconds(entry?.Timings?.Ssl ?? 0);
                var sendStart = connectEnd;
                var sendEnd = sendStart.AddMilliseconds(entry?.Timings?.Send ?? 0);
                var waitStart = sendEnd;
                var waitEnd = waitStart.AddMilliseconds(entry?.Timings?.Wait ?? 0);
                var receiveStart = waitEnd;
                var receiveEnd = receiveStart.AddMilliseconds(entry?.Timings?.Receive ?? 0);
                

                if (!internalConnections.TryGetValue(
                        entry.Connection ?? string.Empty, out var connection)) {
                    internalConnections[(onTheFlyId++).ToString()] = 
                        connection = new ConnectionInfo(
                        connectionId++, new AuthorityInfo(hostName, port, secure), 
                        secure  ? new SslInfo(SslProtocols.None, null, null, null, null,
                            entry.Request.HttpVersion, null, 
                            HashAlgorithmType.None, CipherAlgorithmType.None),1, dnsStart,
                        dnsEnd, connectStart, connectEnd, sslStart, sslEnd, 0, string.Empty, 
                        entry.ServerIPAddress ?? string.Empty, entry.Request.HttpVersion, 

                        )
                }


            }


        }
    }

    public class HarReadModel
    {
        public List<HarReadEntry> Entries { get; set; } = new(); 
    }

    public class HarReadEntry
    {
        public string?  Connection { get; set; }

        // ReSharper disable once InconsistentNaming
        public string ? ServerIPAddress { get; set; }

        public HarReadTiming ? Timings { get; set; }

        public HarReadRequest?  Request { get; set; }

        public HarEntryResponse? Response { get; set; }

        public DateTime StartDateTime { get; set; }

    }

    public class HarReadRequest
    {
        public string Method { get; set; } 

        public string FullUrl { get; set; } 

        public string HttpVersion { get; set; }

        public List<HarReadHeader> Headers { get; set; } = new(); 
    }

    public class HarReadHeader
    {
        public HarReadHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; } 

        public string Value { get; set; }
    }

    public class HarReadResponse
    {
        public int Status { get; set; }

        public string ? StatusText { get; set; }

        public List<HarReadHeader> Headers { get; set; } = new();

        public HarReadResponseContent Content { get; set; } = new(); 
    }

    public class HarReadResponseContent
    {
        public long Size { get; set; }

        public string ? Text { get; set; }
    }

    public class HarReadTiming
    {

        public double Blocked { get; set;  } = -1;

        public double Dns { get; set; }

        public double Connect { get; set; }

        public double Send { get; set; }

        public double Wait { get; set; }

        public double Receive { get; set; }

        public double Ssl { get; set; }
    }
}
