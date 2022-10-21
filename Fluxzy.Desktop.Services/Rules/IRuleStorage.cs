namespace Fluxzy.Desktop.Services.Rules
{
    public interface IRuleStorage
    {
        Task<List<RuleContainer>> ReadRules();

        Task Update(ICollection<RuleContainer> rules);
    }
}
