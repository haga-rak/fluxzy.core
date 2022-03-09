using System;
using Echoes.Clients;

namespace Echoes.Rules
{
    public static class TrafficRuleSettingExtensions
    {
        public static IRuleInstaller When(this ProxyAlterationRule alterationRule,
            Func<Request, bool> filter)
        {
            return new RuleInstaller(alterationRule, filter);
        }

        public static ProxyAlterationRule ReplyWith(this IRuleInstaller ruleInstaller, ReplyContent replyContent)
        {
            var installer = (RuleInstaller) ruleInstaller;
            installer.ParentAlteration.AddByRequestRule(installer.Filter, replyContent);
            return installer.ParentAlteration;
        }
    }
}