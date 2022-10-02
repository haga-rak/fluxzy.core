using System;
using System.Text;
using Fluxzy.Formatters;

namespace Fluxzy.Screeners
{
    internal interface IFormattingProducer<out T>
        where T : FormattingResult
    {
        string ResultTitle { get; }

        T? Build(ExchangeInfo exchangeInfo, ProducerContext context); 
    }
}

