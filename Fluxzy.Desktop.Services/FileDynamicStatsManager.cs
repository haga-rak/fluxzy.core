// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Clients;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class FileDynamicStatsManager : ObservableProvider<DynamicStatistic>
    {
        protected override BehaviorSubject<DynamicStatistic> Subject { get; } = new(new DynamicStatistic(new()));

        public FileDynamicStatsManager(IObservable<TrunkState> trunkStateObservable)
        {
            trunkStateObservable
                .Select(t => t.Agents.ToHashSet())
                .Do(agents => Subject.OnNext(new DynamicStatistic(agents)))
                .Subscribe(); 
        }

        public void ExchangeAdded(Exchange exchangeInfo)
        {
            if (exchangeInfo.Agent != null) {

                var set = Subject.Value.Agents; 

                if (set.Add(exchangeInfo.Agent)) {
                    Subject.OnNext(new DynamicStatistic(set));
                }
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