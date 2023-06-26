// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules;

namespace Fluxzy.Desktop.Services.Rules
{
    public class RuleImportationManager
    {
        private readonly RuleConfigParser _ruleConfigParser;

        public RuleImportationManager(RuleConfigParser ruleConfigParser)
        {
            _ruleConfigParser = ruleConfigParser;
        }

        public string Export(RuleExportSetting ruleExportSetting)
        {
            return _ruleConfigParser.GetYamlFromRuleSet(new RuleSet(ruleExportSetting.Rules.ToArray()));
        }

        public List<Rule> Import(RuleImportSetting ruleImportSetting)
        {
            var containers = InternalImport(ruleImportSetting.GetContent());
            return containers.Select(s => s.Rule).ToList();
        }

        private IEnumerable<RuleContainer> InternalImport(
            string yamlContent)
        {
            var ruleConfigContainer = _ruleConfigParser.TryGetRuleSetFromYaml(yamlContent, out var readErrors);

            if (ruleConfigContainer == null)
            {
                throw new DesktopException($"Failed to import rule: {string.Join(", ",
                    readErrors!.Select(s => s.Message))}");
            }

            return ruleConfigContainer.Rules.SelectMany(t => t.GetAllRules().Select(s => new RuleContainer(s)));
        }
    }
}
