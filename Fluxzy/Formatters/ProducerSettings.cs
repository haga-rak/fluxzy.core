// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Formatters
{
    public class ProducerSettings
    {
        public int MaxFormattableJsonLength { get; set; } = 2 * 1024 * 1024;

        public int MaxFormattableXmlLength { get; set; } = 1024 * 1024;

        public int MaxHeaderLength { get; set; } = 1024 * 48;

        public int MaxMultipartContentStringLength { get; set; } = 1024;

        public int MaximumRenderableBodyLength { get; set; } = 4 * 1024 * 1024; 
    }
}