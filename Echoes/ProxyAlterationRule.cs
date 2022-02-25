using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Echoes.Rules;

namespace Echoes
{
    public class ProxyAlterationRule
    {
        private readonly List<ByRequestRule> _innerRules = new();

        public IReadOnlyCollection<ByRequestRule> ByRequestRules => new ReadOnlyCollection<ByRequestRule>(_innerRules);

        public bool TryGetByRequestRule(Request request, out ReplyContent replyContent)
        {
            var result = ByRequestRules.FirstOrDefault(requestRule => requestRule.Filter(request));
            replyContent = result?.ReplyContent; 
            return result != null;
        }

        public void AddByRequestRule(Func<Request, bool> filter, ReplyContent replyContent)
        {
            _innerRules.Add(new ByRequestRule(filter, replyContent));
        }

        public void AddByRequestRule(ByRequestRule byRequestRule)
        {
            _innerRules.Add(byRequestRule);
        }

        public static ProxyAlterationRule CreateDefault()
        {
            return new ProxyAlterationRule();
        }
    }
}