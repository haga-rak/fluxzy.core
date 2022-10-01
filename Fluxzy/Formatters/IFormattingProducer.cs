using System;
using System.Collections.Generic;
using System.Text;
using Fluxzy.Readers;

namespace Fluxzy.Screeners
{
    internal interface IFormattingProducer<out T>
        where T : FormattingResult
    {
        string ResultTitle { get; }

        T? Build(ExchangeInfo exchangeInfo, FormattingProducerParam producerSetting, IArchiveReader archiveReader); 
    }


    public class FormattingProducerParam
    {
        public int MaxFormattableJsonLength { get; set; } = 1024 * 32;

        public int MaxFormattableXmlLength { get; set; } = 1024 * 32;
    }
}

