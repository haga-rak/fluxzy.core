// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.ContextualFilters
{
    public class QuickActionBuilder : ObservableProvider<QuickActionResult>
    {
        protected override BehaviorSubject<QuickActionResult> Subject { get; } = new(new(new())); 

        public QuickActionBuilder(IObservable<TrunkState> trunkStateObservable,
            IObservable<ContextualFilterResult> contextFilterResult)
        {
            trunkStateObservable.CombineLatest(
                        contextFilterResult)
                                .Select(t => Build(t.First, t.Second))
                                .Do(t => Subject.OnNext(t))
                                .Subscribe();
        }
        
        private QuickActionResult Build(TrunkState _, ContextualFilterResult contextFilterResult)
        {

            var quickActions = contextFilterResult.ContextualFilters
                                                  .Select(s =>
                                                      new QuickAction(s.Filter.Identifier.ToString(),
                                                          "Filter",
                                                          s.Filter.FriendlyName,
                                                          false,
                                                          new QuickActionPayload(s.Filter), QuickActionType.Filter)).ToList();

            return new QuickActionResult(quickActions);
        }

    }


    public class QuickActionResult
    {
        public QuickActionResult(List<QuickAction> actions)
        {
            Actions = actions;
        }

        public List<QuickAction> Actions { get;  }
    }

    
    public class QuickAction
    {
        public QuickAction(string id, 
            string category, 
            string label, 
            bool needExchangeId,
            QuickActionPayload quickActionPayload, QuickActionType type)
        {
            Id = id;
            Category = category;
            Label = label;
            NeedExchangeId = needExchangeId;
            QuickActionPayload = quickActionPayload;
            Type = type;
        }

        /// <summary>
        /// Unique id of the action 
        /// </summary>
        public string Id { get; }

        public QuickActionType Type { get; }

        /// <summary>
        /// Name 
        /// </summary>
        public string Category { get; }
        

        public string Label { get;  } 

        /// <summary>
        /// 
        /// </summary>
        public bool NeedExchangeId { get;  }

        public QuickActionPayload QuickActionPayload { get;  }

        public List<string> Keywords { get; } = new();
    }


    public enum QuickActionType
    {
        BackendCall,
        ClientOperation,
        Filter
    }

    public class QuickActionPayload
    {
        public QuickActionPayload(Filter? filter)
        {
            Filter = filter;
        }

        public Filter? Filter { get; }
    }
}
