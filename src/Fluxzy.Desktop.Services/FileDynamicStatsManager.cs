// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class FileDynamicStatsManager : ObservableProvider<DynamicStatistic>
    {
        public FileDynamicStatsManager(IObservable<TrunkState> trunkStateObservable)
        {
            trunkStateObservable
                .Select(t => t.Agents.ToHashSet())
                .Do(agents => Subject.OnNext(new DynamicStatistic(agents)))
                .Subscribe();
        }

        protected override BehaviorSubject<DynamicStatistic> Subject { get; } =
            new(new DynamicStatistic(new HashSet<Agent>()));

        public void ExchangeAdded(Exchange exchangeInfo)
        {
            if (exchangeInfo.Agent != null) {
                var set = Subject.Value.Agents;

                if (set.Add(exchangeInfo.Agent))
                    Subject.OnNext(new DynamicStatistic(set));
            }
        }
    }

    public class DynamicStatistic
    {
        public DynamicStatistic(HashSet<Agent> agents)
        {
            Agents = agents;
        }

        public HashSet<Agent> Agents { get; set; }
    }
}
