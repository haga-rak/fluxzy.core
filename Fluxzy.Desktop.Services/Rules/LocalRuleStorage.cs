// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.Rules
{
    public class LocalRuleStorage : IRuleStorage
    {
        private readonly DirectoryInfo _filterDirectory;

        public LocalRuleStorage()
        {
            var basePath = Environment.ExpandEnvironmentVariables("%appdata%/Fluxzy.Desktop/rules");
            Directory.CreateDirectory(basePath);
            _filterDirectory = new DirectoryInfo(basePath);

            if (!_filterDirectory.EnumerateFiles("*.rule.json").Any()) {
                Update(new List<RuleContainer> {
                        new(new Rule(new AddRequestHeaderAction("fluxzy-on", "true"), AnyFilter.Default))
                    }
                );
            }
        }

        public Task<List<RuleContainer>> ReadRules()
        {
            var rules = new List<RuleContainer>();

            foreach (var fileInfo in _filterDirectory.EnumerateFiles("*.rule.json")) {
                using var stream = fileInfo.OpenRead();

                rules.Add(JsonSerializer.Deserialize<RuleContainer>(stream,
                    GlobalArchiveOption.DefaultSerializerOptions)!);
            }

            rules = rules.OrderBy(r => r.Rule.Order).ToList();

            FixOrder(rules);

            return Task.FromResult(rules);
        }

        public Task Update(ICollection<RuleContainer> rules)
        {
            FixOrder(rules);

            var dictionaryRules = rules.ToDictionary(t =>
                new FileInfo(GetRulePath(t.Rule)).FullName, t => t);

            foreach (var fileInfo in _filterDirectory.EnumerateFiles("*.rule.json").ToList()) {
                if (!dictionaryRules.ContainsKey(fileInfo.FullName)) {
                    fileInfo.Delete();
                }
            }

            foreach (var rule in rules) {
                using var stream = File.Create(GetRulePath(rule.Rule));
                JsonSerializer.Serialize(stream, rule, GlobalArchiveOption.DefaultSerializerOptions);
            }

            return Task.CompletedTask;
        }

        private static void FixOrder(ICollection<RuleContainer> rules)
        {
            var count = 0;

            foreach (var rule in rules.OrderBy(o => o.Rule.Order)) {
                rule.Rule.Order = ++count;
            }
        }

        private string GetRulePath(Rule rule)
        {
            return Path.Combine(_filterDirectory.FullName, $"{rule.Identifier}.rule.json");
        }
    }
}
