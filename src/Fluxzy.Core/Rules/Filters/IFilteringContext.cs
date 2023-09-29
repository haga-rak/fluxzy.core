// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Readers;

namespace Fluxzy.Rules.Filters
{
    public interface IFilteringContext
    {
        IArchiveReader Reader { get; }

        bool HasRequestBody { get; }
    }

    public class ExchangeInfoFilteringContext : IFilteringContext
    {
        private readonly int _exchangeId;

        public ExchangeInfoFilteringContext(IArchiveReader reader, int exchangeId)
        {
            Reader = reader;
            _exchangeId = exchangeId;
        }

        public bool HasRequestBody => Reader.HasRequestBody(_exchangeId);

        public IArchiveReader Reader { get; }
    }
}
