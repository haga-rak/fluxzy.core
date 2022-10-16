// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Filters;
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

        [HttpGet("templates")]
        public ActionResult<List<FilterTemplate>> GetTemplates([FromServices] FilterTemplateManager templateManager)
        {
            return templateManager.ReadAvailableTemplates();
        }


        [HttpPost("apply-to-view")]
        public ActionResult<bool> ApplyToView(Filter filter, [FromServices] ActiveViewFilterManager activeViewFilterManager)
        {
            activeViewFilterManager.Update(new ViewFilter(filter));
            return true;
        }

    }
}