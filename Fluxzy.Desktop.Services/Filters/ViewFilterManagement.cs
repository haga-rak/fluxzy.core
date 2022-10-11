using Fluxzy.Desktop.Services.Filters.Implementations;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters
{
    public class ViewFilterManagement
    {
        private readonly IReadOnlyCollection<IFilterStorage> _storages;

        public ViewFilterManagement(LocalFilterStorage localFileStorage, InSessionFileStorage inSessionFileStorage)
        {
            _storages = new IFilterStorage [] { localFileStorage, inSessionFileStorage }; 
        }

        public IEnumerable<StoredFilter> Get()
        {
            return _storages
                .Select(storage => new StoredFilter(storage.StoreLocation, storage.Get())).ToList(); 
        }

        public void AddOrUpdate(Guid filterId, StoreLocation storeLocation, Filter filter)
        {
            var targetStore = _storages.First(s => s.StoreLocation == storeLocation);

            targetStore.AddOrUpdate(filterId , filter); 
        }

        public bool Delete(Guid filterId, StoreLocation storeLocation)
        {
            var targetStore = _storages.First(s => s.StoreLocation == storeLocation);
            return targetStore.Remove(filterId); 
        }
    }


    public class StoredFilter
    {
        public StoredFilter(StoreLocation storeLocation, IEnumerable<Filter> filters)
        {
            StoreLocation = storeLocation;
            Filters = filters.ToList();
        }

        public StoreLocation StoreLocation { get;  }

        public List<Filter> Filters { get;  }
    }

}
