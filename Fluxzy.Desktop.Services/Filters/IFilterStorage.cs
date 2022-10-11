// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Filters
{
    public interface IFilterStorage
    {
        StoreLocation StoreLocation { get;  }

        IEnumerable<Filter> Get(); 

        bool Remove(Guid filterId);

        bool TryGet(Guid filterId, out Filter? filter); 
        
        void AddOrUpdate(Guid filterId, Filter updatedContent);
    }
}