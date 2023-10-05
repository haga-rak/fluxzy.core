// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Cli.Commands.Dissects
{
    internal interface IDissectionFilter
    {
        bool IsApplicable(ExchangeInfo exchangeInfo, ConnectionInfo connectionInfo);
    }
}
