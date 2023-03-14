// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Rules;
using Fluxzy.Rules;

namespace Fluxzy.Desktop.Services
{
    public class ActiveRuleManager : ObservableProvider<HashSet<Guid>>
    {
        private readonly IRuleStorage _ruleStorage;

        /// <summary>
        /// Rules that stays on memory (not persisted), mostly breakpoints
        /// </summary>
        private readonly BehaviorSubject<ImmutableList<Rule>> _inMemoryRules = new(ImmutableList.Create<Rule>()); 

        public ActiveRuleManager(IRuleStorage ruleStorage)
        {
            _ruleStorage = ruleStorage;
            var subject = new BehaviorSubject<HashSet<Guid>>(new HashSet<Guid>());

            ActiveRules =
                _inMemoryRules.AsObservable()
                              .CombineLatest(
                                  subject.AsObservable()
                                         .Select(s => Observable.FromAsync(async () => {
                                             var ruleContainers = await ruleStorage.ReadRules();

                                             // TODO : unload breakpoint actions here 

                                             return ruleContainers.Where(r => s.Contains(r.Rule.Identifier))
                                                                  .Select(r => r.Rule).ToList();
                                         })).Concat())
                              .Select(s => s.Second.Concat(s.First).ToList()); ;

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

        public void AddInMemoryRule(Rule rule)
        {
            _inMemoryRules.OnNext(_inMemoryRules.Value.Add(rule));
        }

        public void RemoveInMemoryRule(Guid filterIdentifier)
        {
            _inMemoryRules.OnNext(_inMemoryRules.Value.RemoveAll(t => t.Filter.Identifier == filterIdentifier));
        }

        public void ClearInMemoryRules()
        {
            _inMemoryRules.OnNext(ImmutableList.Create<Rule>());
        }

        public IEnumerable<Rule> GetInMemoryRules()
        {
            return _inMemoryRules.Value;
        }
    }
}
