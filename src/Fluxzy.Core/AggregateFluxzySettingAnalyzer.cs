// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Validators;
using System.Collections.Generic;

namespace Fluxzy
{
    public class AggregateFluxzySettingAnalyzer : ISettingAnalyzer
    {
        public static ISettingAnalyzer Instance { get; } = new AggregateFluxzySettingAnalyzer();

        private static readonly ISettingAnalyzer[] Validators = new ISettingAnalyzer[]
        {
            new RuleCountValidator(),
            new SkipSslEnableValidator(),
            new OutOfScopeValidator(),
            new ActionValidator()
        };

        public IEnumerable<ValidationResult> Validate(FluxzySetting setting)
        {
            foreach (var validator in Validators)
            {
                foreach (var result in validator.Validate(setting))
                {
                    yield return result;
                }
            }
        }
    }

    public enum ValidationRuleLevel
    {
        Information,
        Warning,
        Error,
        Fatal
    }

    public interface ISettingAnalyzer
    {
        IEnumerable<ValidationResult> Validate(FluxzySetting setting);
    }

    public class ValidationResult
    {
        public ValidationResult(ValidationRuleLevel level, string message, string senderName)
        {
            Level = level;
            Message = message;
            SenderName = senderName;
        }

        public ValidationRuleLevel Level { get;  }

        public string Message { get; }

        public string SenderName { get; }

        public override string ToString()
        {
            return $"[{Level}] [{SenderName}]: {Message}";
        }
    }
}
