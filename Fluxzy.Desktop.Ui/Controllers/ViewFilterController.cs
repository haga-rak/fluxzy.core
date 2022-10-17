// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Filters;
using Fluxzy.Rules.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/view-filter")]
    [ApiController]
    public class ViewFilterController : ControllerBase
    {
        private readonly ViewFilterManagement _management;

        public ViewFilterController(ViewFilterManagement management)
        {
            _management = management;
        }

        [HttpGet()]
        public ActionResult<IEnumerable<StoredFilter>> Get()
        {
            return new ActionResult<IEnumerable<StoredFilter>>(_management.Get()); 
        }

        [HttpPut("{filterId}/store/{storeLocation}")]
        public ActionResult<bool> Update(Guid filterId, StoreLocation storeLocation, Filter filter )
        {
            _management.AddOrUpdate(filterId, storeLocation, filter);
            return true; 
        }

        [HttpPost("store/{storeLocation}")]
        public ActionResult<bool> Add(StoreLocation storeLocation, Filter filter )
        {
            _management.AddOrUpdate(filter.Identifier, storeLocation, filter);
            return true; 
        }

        [HttpDelete("{filterId}/store/{storeLocation}")]
        public ActionResult<bool> Delete(Guid filterId, StoreLocation storeLocation)
        {
            return _management.Delete(filterId, storeLocation); 
        }


        [HttpPatch("store")]
        public ActionResult<bool> Patch(List<LocatedFilter> filters)
        {
            return _management.Patch(filters); 
        }
    }
}