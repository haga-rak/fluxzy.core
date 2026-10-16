// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Cli.Commands.Dissects.Formatters
{
    internal static class FormatterRegistration
    {
        public static readonly IReadOnlyCollection<IDissectionFormatter<EntryInfo>>
            Formatters = new List<IDissectionFormatter<EntryInfo>> {
                new UrlFormatter(),
                new MethodFormatter(),
                new StatusCodeFormatter(),
                new ContentTypeFormatter(),
                new AuthorityFormatter(),
                new PathFormatter(),
                new HostFormatter(),
                new IdFormatter(),
                new HttpVersionFormatter(),
                new SchemeFormatter(),
                new RequestBodyLengthFormatter(),
                new ResponseBodyLengthFormatter(),
                new ResponseBodyFormatter(),
                new RequestBodyFormatter(),
                new ResponseBodyDecodedFormatter(),
                new RequestBodyDecodedFormatter(),
                new RequestHeadersFormatter(),
                new ResponseHeadersFormatter(),
                new RequestHeaderFormatter(),
                new ResponseHeaderFormatter(),
                new PcapFormatter(),
                new PcapRawFormatter()
            }.Concat(MetaFormatters.All()).ToList();

        public static IReadOnlyCollection<string> Indicators { get; } = Formatters.Select(s => s.Indicator)
            .ToList();
    }
}
