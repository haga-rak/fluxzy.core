// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Validators
{
    internal class RuleCountValidator : ISettingAnalyzer
    {
        public IEnumerable<ValidationResult> Validate(FluxzySetting setting)
        {
            var actionCount = setting.AlterationRules.Count;

            if (actionCount == 0) {
                yield break;
            }

            yield return new ValidationResult(
                ValidationRuleLevel.Information,
                $"{actionCount} action(s) loaded",
                GetType().Name
            );
        }
    }

    internal class SkipSslEnableValidator : ISettingAnalyzer
    {
        public IEnumerable<ValidationResult> Validate(FluxzySetting setting)
        {
            if (!setting.GlobalSkipSslDecryption) {
                yield break;
            }

            var actionCount = setting.AlterationRules.Count;

            if (actionCount == 0) {
                yield break;
            }

            yield return new ValidationResult(
                ValidationRuleLevel.Warning,
                $"{actionCount} action(s) will be ignored because GlobalSkipSslDecryption is enabled.",
                GetType().Name
            );
        }
    }

    internal class OutOfScopeValidator : ISettingAnalyzer
    {
        public IEnumerable<ValidationResult> Validate(FluxzySetting setting)
        {
            if (setting.GlobalSkipSslDecryption) {
                yield break;
            }

            var outOfScopeRules = setting.AlterationRules.Where(c => !c.InScope).ToList();

            var allNames = string.Join(", ", outOfScopeRules.Select(c => $"[{c}]"));

            if (outOfScopeRules.Any()) {
                yield return new ValidationResult(
                    ValidationRuleLevel.Warning,
                    $"{outOfScopeRules} action(s) will be ignored because they are out of scope (filter can only be evaluated " +
                    $"after the action scope): {allNames}",
                    GetType().Name
                );
            }
        }
    }

    internal class ActionValidator : ISettingAnalyzer
    {
        public IEnumerable<ValidationResult> Validate(FluxzySetting setting)
        {
            if (setting.GlobalSkipSslDecryption) {
                yield break;
            }

            foreach (var rule in setting.AlterationRules) {
                foreach (var validateResult in rule.Action.Validate(setting, rule.Filter)) {
                    yield return validateResult;
                }
            }
        }
    }
}
