// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Rules;
using Fluxzy.Desktop.Ui.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActionController : ControllerBase
    {
        [HttpGet("description/{typeKind}")]
        public ActionResult<DescriptionInfo> GetFilterDescription(
            string typeKind, [FromServices] ActionTemplateManager templateManager)
        {
            if (templateManager.TryGetDescription(typeKind, out var longDescription))
                return new DescriptionInfo(longDescription);

            return NotFound();
        }
    }
}
