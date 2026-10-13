// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading.Tasks;
using Fluxzy.Core.Pcap.Pcapng;
using Fluxzy.Misc.Streams;
using Fluxzy.Utils;

namespace Fluxzy.Cli.Commands.Dissects.Formatters
{
    internal class UrlFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "url";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange?.FullUrl ?? string.Empty);
        }
    }

    internal class MethodFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "method";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange?.Method ?? string.Empty);
        }
    }

    internal class StatusCodeFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "status";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            if (payload.Exchange.StatusCode == 0) {
                return Task.CompletedTask;
            }

            return stdOutWriter.WriteAsync(payload.Exchange.StatusCode.ToString() ?? string.Empty);
        }
    }

    internal class ContentTypeFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "content-type";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange?.ContentType ?? string.Empty);
        }
    }

    internal class AuthorityFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "authority";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange.KnownAuthority);
        }
    }

    internal class PathFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "path";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange?.Path ?? string.Empty);
        }
    }

    internal class HostFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "host";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange.KnownAuthority);
        }
    }

    internal class IdFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "id";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange.Id.ToString());
        }
    }

    internal class HttpVersionFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "http-version";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange.HttpVersion);
        }
    }

    internal class SchemeFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "scheme";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(payload.Exchange.RequestHeader.Scheme);
        }
    }

    internal class RequestBodyLengthFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "request-body-length";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            var requestBodyLength = payload.ArchiveReader.GetRequestBodyLength(payload.Exchange.Id);

            return stdOutWriter.WriteAsync(requestBodyLength.ToString());
        }
    }

    internal class ResponseBodyLengthFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "response-body-length";

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            var responseBodyLength = payload.ArchiveReader.GetResponseBodyLength(payload.Exchange.Id);

            return stdOutWriter.WriteAsync(responseBodyLength.ToString());
        }
    }

    internal abstract class BodyFormatter : IDissectionFormatter<EntryInfo>
    {
        public abstract string Indicator { get; }

        protected abstract Stream? Open(EntryInfo payload);

        public async Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            await using var bodyStream = Open(payload);

            if (bodyStream == null) {
                return;
            }

            await stdOutWriter.FlushAsync();

            if (payload.MaxBodyBytes == null) {
                await bodyStream.CopyToAsync(stdOutWriter.BaseStream);

                return;
            }

            await CopyTruncated(bodyStream, stdOutWriter, payload.MaxBodyBytes.Value);
        }

        private static async Task CopyTruncated(Stream source, StreamWriter stdOutWriter, int max)
        {
            var buffer = new byte[16 * 1024];
            long written = 0;
            long total = 0;
            int read;

            while ((read = await source.ReadAsync(buffer)) > 0) {
                total += read;

                var remaining = (int) Math.Min(read, Math.Max(0, max - written));

                if (remaining > 0) {
                    await stdOutWriter.BaseStream.WriteAsync(buffer.AsMemory(0, remaining));
                    written += remaining;
                }
            }

            if (total > written) {
                await stdOutWriter.WriteAsync($"\n[truncated: {written} of {total} bytes]");
                await stdOutWriter.FlushAsync();
            }
        }
    }

    internal class ResponseBodyFormatter : BodyFormatter
    {
        public override string Indicator => "response-body";

        protected override Stream? Open(EntryInfo payload)
        {
            return payload.ArchiveReader.GetResponseBody(payload.Exchange.Id);
        }
    }

    internal class RequestBodyFormatter : BodyFormatter
    {
        public override string Indicator => "request-body";

        protected override Stream? Open(EntryInfo payload)
        {
            return payload.ArchiveReader.GetRequestBody(payload.Exchange.Id);
        }
    }

    internal class ResponseBodyDecodedFormatter : BodyFormatter
    {
        public override string Indicator => "response-body-decoded";

        protected override Stream? Open(EntryInfo payload)
        {
            return payload.ArchiveReader.GetDecodedResponseBody(payload.Exchange.Id);
        }
    }

    internal class RequestBodyDecodedFormatter : BodyFormatter
    {
        public override string Indicator => "request-body-decoded";

        protected override Stream? Open(EntryInfo payload)
        {
            return payload.ArchiveReader.GetDecodedRequestBody(payload.Exchange.Id);
        }
    }

    internal class PcapFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "pcap";

        public async Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            await using var pcapStream = payload.ArchiveReader.GetRawCaptureStream(payload.Connection?.Id ?? 0);

            if (pcapStream == null) {
                return;
            }

            // Extract SSL key log file 
            var sslKeyLogContent = payload.ArchiveReader.GetRawCaptureKeyStream(payload.Connection?.Id ?? 0)
                                          ?.ReadToEndGreedy();

            if (sslKeyLogContent == null) {
                await pcapStream.CopyToAsync(stdOutWriter.BaseStream);

                return;
            }

            if (pcapStream.CanSeek) {
                await PcapngUtils.CreatePcapngFileWithKeysAsync(sslKeyLogContent, pcapStream!, stdOutWriter.BaseStream);

                return;
            }

            var tempFile = ExtendedPathHelper.GetTempFileName();

            await using (var tempFileStream = File.Create(tempFile)) {
                await pcapStream.CopyToAsync(tempFileStream);
            }
            
            try {
                await using var inStream = File.OpenRead(tempFile);
                await PcapngUtils.CreatePcapngFileWithKeysAsync(sslKeyLogContent, inStream, stdOutWriter.BaseStream);
            }
            finally {
                File.Delete(tempFile);
            }
        }
    }

    internal class PcapRawFormatter : IDissectionFormatter<EntryInfo>
    {
        public string Indicator => "pcap-raw";

        public async Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            var pcapStream = payload.ArchiveReader.GetRawCaptureStream(payload.Connection?.Id ?? 0);

            if (pcapStream == null) {
                return;
            }

            await pcapStream.CopyToAsync(stdOutWriter.BaseStream);
        }
    }
}
