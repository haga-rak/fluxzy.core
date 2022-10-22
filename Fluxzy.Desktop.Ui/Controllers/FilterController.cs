// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Filters;
using Fluxzy.Desktop.Services.Models;
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

        [HttpGet("templates/any")]
        public ActionResult<AnyFilter> GetTemplates()
        {
            return AnyFilter.Default;
        }

        [HttpPost("apply-to-view")]
        public ActionResult<bool> ApplyToView(Filter filter, [FromServices] ActiveViewFilterManager activeViewFilterManager,
            [FromServices] TemplateToolBarFilterProvider filterProvider)
        {
            activeViewFilterManager.Update(new ViewFilter(filter));
            filterProvider.SetNewFilter(filter);

            return true;
        }
    }
}