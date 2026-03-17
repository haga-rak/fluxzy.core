// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Formatters
{
    public class FormatSettings
    {
        public static FormatSettings Default { get; } = new();

        public int MaxFormattableJsonLength { get; set; } = 2 * 1024 * 1024;

        public int MaxFormattableXmlLength { get; set; } = 1024 * 1024;

        public int MaxHeaderLength { get; set; } = 1024 * 48;

        public int MaxMultipartContentStringLength { get; set; } = 1024;

        public int MaximumRenderableBodyLength { get; set; } = 4 * 1024 * 1024;

        public int MaxFormattableProtobufLength { get; set; } = 2 * 1024 * 1024;

        public List<string> ProtoDirectories { get; set; } = new();
    }
}
