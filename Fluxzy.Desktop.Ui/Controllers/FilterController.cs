// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Rules.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilterController : ControllerBase
    {
        [HttpPost("validate")]
        public ActionResult<Filter> Validate(Filter filter)
        {
            return filter; 
        }
    }
}