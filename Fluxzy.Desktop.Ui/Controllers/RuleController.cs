// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Rules;
using Microsoft.AspNetCore.Mvc;
using Action = Fluxzy.Rules.Action;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RuleController
    {
        private readonly IRuleStorage _ruleStorage;

        public RuleController(IRuleStorage ruleStorage)
        {
            _ruleStorage = ruleStorage;
        }

        [HttpGet("container")]
        public async Task<ActionResult<List<RuleContainer>>> Get()
        {
            return await _ruleStorage.ReadRules();
        }

        [HttpPost("container")]
        public async Task<ActionResult<bool>> Patch([FromBody] RuleContainer[] containers)
        {
            await _ruleStorage.Update(containers);
            return true; 
        }

        [HttpPost("action/validation")]
        public ActionResult<Fluxzy.Rules.Action> Patch([FromBody] Fluxzy.Rules.Action action)
        {
            return action; 
        }


        [HttpGet("action")]
        public ActionResult<List<Action>> GetActionTemplates([FromServices] ActionTemplateManager templateManager)
        {
            return templateManager.GetDefaultActions();
        }
    }
}