namespace Fluxzy.Rules.Extensions
{
    /// <summary>
    ///  Helper to build alteration rules in a fluent way
    /// </summary>
    public interface IConfigureActionBuilder
    {
        /// <summary>
        ///  Add one or more actions to the rule
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        IConfigureFilterBuilder Do(Action action, params Action [] actions);
    }
}