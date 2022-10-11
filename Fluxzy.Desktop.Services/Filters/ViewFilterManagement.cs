using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters
{
    public class ViewFilterManagement
    {
        private readonly IEnumerable<IFilterStorage> _storages;

        public ViewFilterManagement(IEnumerable<IFilterStorage> storages)
        {
            _storages = storages;
        }

        public IEnumerable<StoredFilter> Get()
        {
            return _storages
                .Select(storage => new StoredFilter(storage.StoreLocation, storage.Get())).ToList(); 
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
