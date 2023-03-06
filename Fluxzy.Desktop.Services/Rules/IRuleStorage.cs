// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Rules
{
    public interface IRuleStorage
    {
        Task<List<RuleContainer>> ReadRules();

        Task Update(ICollection<RuleContainer> rules);
    }
}
