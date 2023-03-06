// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters.Implementations
{
    public class InSessionFileStorage : IFilterStorage
    {
        private Dictionary<Guid, Filter> _currentFilters = new();

        public StoreLocation StoreLocation => StoreLocation.OnSession;

        public IEnumerable<Filter> Get()
        {
            return _currentFilters.Values.ToList();
        }

        public bool Remove(Guid filterId)
        {
            return _currentFilters.Remove(filterId);
        }

        public bool TryGet(Guid filterId, out Filter? filter)
        {
            return _currentFilters.TryGetValue(filterId, out filter);
        }

        public void AddOrUpdate(Guid filterId, Filter updatedContent)
        {
            _currentFilters[filterId] = updatedContent;
        }

        public void Patch(IEnumerable<Filter> filters)
        {
            _currentFilters = filters.DistinctBy(t => t.Identifier)
                                     .ToDictionary(t => t.Identifier, t => t);
        }
    }
}
