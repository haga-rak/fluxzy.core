// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Screeners
{
    public class ProducerSettings
    {
        public int MaxFormattableRequestBody { get; set; } = 1024 * 32;

        public int MaxFormattableJsonLength { get; set; } = 1024 * 32;

        public int MaxFormattableXmlLength { get; set; } = 1024 * 32;

        public int MaxHeaderLength { get; set; } = 1024 * 48; 
    }
}