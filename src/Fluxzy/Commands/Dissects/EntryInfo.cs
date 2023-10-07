// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands.Dissects
{
    internal record EntryInfo(ExchangeInfo Exchange, ConnectionInfo? Connection, IArchiveReader ArchiveReader);
}
