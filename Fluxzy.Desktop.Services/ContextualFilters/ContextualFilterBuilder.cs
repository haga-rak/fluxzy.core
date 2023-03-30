using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Desktop.Services.ContextualFilters
{
    internal class ContextualFilterBuilder : ObservableProvider<ContextualFilterResult>
    {
        protected override BehaviorSubject<ContextualFilterResult> Subject { get; } = new(new());
        
        public ContextualFilterBuilder(IObservable<TrunkState> trunkStateObservable)
        {
            trunkStateObservable
                .Select(Build)
                .Do(t => Subject.OnNext(t))
                .Subscribe(); 
        }

        private ContextualFilterResult Build(TrunkState trunkState)
        {
            var result = new ContextualFilterResult();

            // Most used hostname 

            var mostUsedHostNames = trunkState.Exchanges
                                    .GroupBy(r => r.ExchangeInfo.KnownAuthority)
                                    .OrderByDescending(r => r.Count())
                                    .Take(10)
                                    .Select(r => new ContextualFilter(new HostFilter(r.Key, StringSelectorOperation.Exact), r.Count()));

            result.ContextualFilters.AddRange(mostUsedHostNames);

            // Most used 
            
            // Here we trigger the contextual filter

            return result;
        }

    }


    public class ContextualFilterResult
    {
        public List<ContextualFilter> ContextualFilters { get; } = new(); 


    }

    public class ContextualFilter
    {
        public ContextualFilter(Filter filter, int weight)
        {
            Filter = filter;
            Weight = weight;
        }

        public Filter Filter { get;  }
        
        public int Weight { get; }
    }

}
