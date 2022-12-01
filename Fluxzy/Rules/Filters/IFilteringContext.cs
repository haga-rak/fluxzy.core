// Copyright © 2022 Haga Rakotoharivelo

using System.IO;
using Fluxzy.Readers;

namespace Fluxzy.Rules.Filters
{
    public interface IFilteringContext
    {
        IArchiveReader Reader { get;  }
        
        bool HasRequestBody { get; }
    }

    public class ExchangeInfoFilteringContext : IFilteringContext
    {
        private readonly IArchiveReader _reader;
        private readonly int _exchangeId;

        public ExchangeInfoFilteringContext(IArchiveReader reader, int exchangeId)
        {
            _reader = reader;
            _exchangeId = exchangeId;
        }

        public bool HasRequestBody => _reader.HasRequestBody(_exchangeId);

        public IArchiveReader Reader => _reader;
    }
}