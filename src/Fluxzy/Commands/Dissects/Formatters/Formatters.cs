// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Cli.Commands.Dissects.Formatters;

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
        if (payload.Exchange.StatusCode == 0)
            return Task.CompletedTask;

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

internal class ResponseBodyFormatter : IDissectionFormatter<EntryInfo>
{
    public string Indicator => "response-body";

    public async Task Write(EntryInfo payload, StreamWriter stdOutWriter)
    {
        var responseBodyStream = payload.ArchiveReader.GetResponseBody(payload.Exchange.Id);

        if (responseBodyStream == null)
            return;

        await responseBodyStream.CopyToAsync(stdOutWriter.BaseStream);
    }
}

internal class RequestBodyFormatter : IDissectionFormatter<EntryInfo>
{
    public string Indicator => "request-body";

    public async Task Write(EntryInfo payload, StreamWriter stdOutWriter)
    {
        var requestBodyStream = payload.ArchiveReader.GetRequestBody(payload.Exchange.Id);

        if (requestBodyStream == null)
            return;

        await requestBodyStream.CopyToAsync(stdOutWriter.BaseStream);
    }
}

internal class PcapFormatter : IDissectionFormatter<EntryInfo>
{
    public string Indicator => "pcap";

    public async Task Write(EntryInfo payload, StreamWriter stdOutWriter)
    {
        var pcapStream = payload.ArchiveReader.GetRawCaptureStream(payload.Connection?.Id ?? 0);

        if (pcapStream == null)
            return;

        await pcapStream.CopyToAsync(stdOutWriter.BaseStream);
    }
}