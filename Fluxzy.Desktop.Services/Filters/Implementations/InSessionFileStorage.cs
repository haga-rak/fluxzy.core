using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters.Implementations
{
    public class InSessionFileStorage : IFilterStorage
    {
        private readonly Dictionary<Guid, Filter> _currentFilters = new();

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
    }
}