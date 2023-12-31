﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Formatters
{
    internal interface IFormattingProducer<out T>
        where T : FormattingResult
    {
        string ResultTitle { get; }

        T? Build(ExchangeInfo exchangeInfo, ProducerContext context);
    }
}
