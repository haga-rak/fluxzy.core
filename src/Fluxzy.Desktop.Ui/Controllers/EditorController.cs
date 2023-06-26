// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Ui.ViewModels;
using Fluxzy.Rules;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/editor")]
    public class EditorController : ControllerBase
    {
        private readonly RuleConfigParser _ruleConfigParser;

        public EditorController(RuleConfigParser ruleConfigParser)
        {
            _ruleConfigParser = ruleConfigParser;
        }

        [HttpPost("serialize")]
        public ActionResult<RuleEditorSerializeResult> Serialize([FromBody] Rule rule)
        {
            var yamlResult = _ruleConfigParser.GetYamlFromRule(rule);
            return new RuleEditorSerializeResult(yamlResult); 
        }

        [HttpPost("deserialize")]
        public ActionResult<RuleEditorDeserializeResult> Deserialize(
            [FromBody] RuleEditorDeserializeRequest request)
        {
            var rule = _ruleConfigParser.TryGetRuleFromYaml(request.Content, out var readErrors);

            if (rule == null) {
                return new RuleEditorDeserializeResult(readErrors!);
            }

            var allRule = rule.GetAllRules().ToArray();

            if (allRule.Length > 1) {
                return new RuleEditorDeserializeResult(new List<RuleConfigReaderError>() {
                    new RuleConfigReaderError("Only one rule is allowed")
                });
            }

            return new RuleEditorDeserializeResult(allRule.First());
        }
    }
}
