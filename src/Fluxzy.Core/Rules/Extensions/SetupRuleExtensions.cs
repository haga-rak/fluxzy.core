namespace Fluxzy.Rules.Extensions
{
    /// <summary>
    ///  Contains extensions method for adding alteration rules in a fluent way
    /// </summary>
    public static class SetupRuleExtensions
    {
        /// <summary>
        ///  Set up a new rule adding chain
        /// </summary>
        /// <param name="setting">The fluxzy setting</param>
        /// <returns></returns>
        public static IConfigureFilterBuilder SetupRule(this FluxzySetting setting)
        {
            var addFilter = new ConfigureFilterBuilderBuilder(setting); 
            return addFilter;
        }
    }
}
