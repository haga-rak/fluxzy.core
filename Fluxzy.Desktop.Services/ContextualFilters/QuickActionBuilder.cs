// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services.ContextualFilters
{
    public class QuickActionBuilder : ObservableProvider<QuickActionResult>
    {
        private readonly ToolBarFilterProvider _toolBarFilterProvider;

        protected override BehaviorSubject<QuickActionResult> Subject { get; } = new(new(new())); 

        public QuickActionBuilder(IObservable<TrunkState> trunkStateObservable,
            IObservable<ContextualFilterResult> contextFilterResult, ToolBarFilterProvider toolBarFilterProvider)
        {
            _toolBarFilterProvider = toolBarFilterProvider;

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
        
        /// <summary>
        /// Actions that are unchanged 
        /// </summary>
        /// <returns></returns>
        public QuickActionResult GetStaticQuickActions()
        {
            var listActions = new List<QuickAction>();

            listActions.AddRange(
                _toolBarFilterProvider.GetDefault().Select(f =>
                    new QuickAction(f.Filter.Identifier.ToString(),
                        "Filter",
                        f.Filter.FriendlyName,
                        false,
                        new QuickActionPayload(f.Filter), QuickActionType.Filter)));


            return new QuickActionResult(listActions); 
        }
    }
}
