// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services;
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
        public async Task<ActionResult<bool>> Patch(
            [FromBody] RuleContainer[] containers, [FromServices] ActiveRuleManager activeRuleManager)
        {
            await _ruleStorage.Update(containers);

            activeRuleManager.SetCurrentSelection(
                containers.Where(c => c.Enabled).Select(c => c.Rule.Identifier));

            return true;
        }

        [HttpPost("container/add")]
        public async Task<ActionResult<bool>> AddToExisting(
            [FromBody] Rule rule,
            [FromServices]
            ActiveRuleManager activeRuleManager)
        {
            var currentRuleContainers = await _ruleStorage.ReadRules();
            currentRuleContainers.Add(new RuleContainer(rule, true));

            await _ruleStorage.Update(currentRuleContainers);

            activeRuleManager.SetCurrentSelection(
                currentRuleContainers.Where(c => c.Enabled).Select(c => c.Rule.Identifier));

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
