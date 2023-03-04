﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters
{
    public interface IFilterStorage
    {
        StoreLocation StoreLocation { get; }

        IEnumerable<Filter> Get();

        bool Remove(Guid filterId);

        bool TryGet(Guid filterId, out Filter? filter);

        void AddOrUpdate(Guid filterId, Filter updatedContent);

        void Patch(IEnumerable<Filter> filters);
    }
}
