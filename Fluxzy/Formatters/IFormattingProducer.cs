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

        T? Build(ExchangeInfo exchangeInfo, IArchiveReader archiveReader); 
    }
}
