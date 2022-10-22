// Copyright © 2022 Haga Rakotoharivelo

using System.IO;
using Fluxzy.Readers;

namespace Fluxzy.Rules.Filters
{
    public interface IFilteringContext
    {
        bool HasRequestBody { get; }

        Stream? OpenRequestBody();

        bool HasResponseBody { get; }

        Stream? OpenResponseBody();
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

        public Stream? OpenRequestBody()
        {
            return _reader.GetRequestBody(_exchangeId);
        }

        public bool HasResponseBody => _reader.HasResponseBody(_exchangeId);

        public Stream? OpenResponseBody()
        {
            return _reader.GetResponseBody(_exchangeId);
        }
    }
}