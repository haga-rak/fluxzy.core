// Copyright © 2022 Haga RAKOTOHARIVELO

using Fluxzy.Desktop.Services.Rules;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActionController : ControllerBase
    {

        [HttpGet("{typeKind}")]
        public ActionResult<string> GetFilterDescription(string typeKind, [FromServices] ActionTemplateManager templateManager)
        {
            if (templateManager.TryGetDescription(typeKind, out var longDescription))
            {
                return longDescription;
            }

            return NotFound();
        }
    }
}
