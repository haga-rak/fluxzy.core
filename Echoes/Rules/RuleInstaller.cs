using System;

namespace Echoes.Rules
{
    internal class RuleInstaller : IRuleInstaller
    {
        internal ProxyAlterationRule ParentAlteration { get; }

        internal RuleInstaller(ProxyAlterationRule parentAlteration, Func<Request, bool> filter)
        {
            ParentAlteration = parentAlteration;
            Filter = filter;
        }

        internal Func<Request, bool> Filter { get; }
    }
}