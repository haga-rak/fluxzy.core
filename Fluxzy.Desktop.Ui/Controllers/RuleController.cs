// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Rules;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;
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
        public ActionResult<Action> Patch([FromBody] Action action)
        {
            return action; 
        }


        [HttpPost("validation")]
        public ActionResult<Rule> ValidateRule([FromBody] Rule rule)
        {
            return rule; 
        }

        [HttpPost]
        public ActionResult<Rule> CreateRule([FromBody] Action action)
        {
            return new Rule(
                action,
                new AnyFilter() // TODO build better intel here 
            ); 
        }


        [HttpGet("action")]
        public ActionResult<List<Action>> GetActionTemplates([FromServices] ActionTemplateManager templateManager)
        {
            return templateManager.GetDefaultActions();
        }
    }
}