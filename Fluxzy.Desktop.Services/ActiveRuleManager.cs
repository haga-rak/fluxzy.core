// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Rules;
using Fluxzy.Rules;

namespace Fluxzy.Desktop.Services
{
    public class ActiveRuleManager : ObservableProvider<HashSet<Guid>>
    {
        private readonly IRuleStorage _ruleStorage;

        public ActiveRuleManager(IRuleStorage ruleStorage)
        {
            _ruleStorage = ruleStorage;
            var subject = new BehaviorSubject<HashSet<Guid>>(new HashSet<Guid>());

            ActiveRules = subject.AsObservable()
                                 .Select(s => Observable.FromAsync(async () => {
                                     var ruleContainers = await ruleStorage.ReadRules();

                                     return ruleContainers.Where(r => s.Contains(r.Rule.Identifier))
                                                          .Select(r => r.Rule).ToList();
                                 })).Concat();

            Subject = subject;
        }

        protected override BehaviorSubject<HashSet<Guid>> Subject { get; }

        public IObservable<List<Rule>> ActiveRules { get; }

        public async Task InitRules()
        {
            var rules = await _ruleStorage.ReadRules();

            var selectRuleIds = rules.Where(r => r.Enabled)
                                     .Select(s => s.Rule.Identifier);

            SetCurrentSelection(selectRuleIds);
        }

        public void SetCurrentSelection(IEnumerable<Guid> guids)
        {
            var current = new HashSet<Guid>(guids);

            if (current.SetEquals(Subject.Value))
                return;

            Subject.OnNext(current);
        }
    }
}
