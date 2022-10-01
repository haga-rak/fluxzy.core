using System;
using System.Text;
using Fluxzy.Readers;

namespace Fluxzy.Screeners
{
    internal interface IFormattingProducer<out T>
        where T : FormattingResult
    {
        string ResultTitle { get; }

        T? Build(ExchangeInfo exchangeInfo, ProducerSettings producerSetting, IArchiveReader archiveReader); 
    }


    public class ProducerSettings
    {
        public int MaxFormattableJsonLength { get; set; } = 1024 * 32;

        public int MaxFormattableXmlLength { get; set; } = 1024 * 32;
    }
}

