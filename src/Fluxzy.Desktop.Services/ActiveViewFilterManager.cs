// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services
{
    public class ActiveViewFilterManager : ObservableProvider<ViewFilter>
    {
        protected override BehaviorSubject<ViewFilter> Subject { get; } =
            new(new ViewFilter(AnyFilter.Default, AnyFilter.Default));

        public ViewFilter Current => Subject.Value;

        public void UpdateViewFilter(Filter filter)
        {
            var current = Subject.Value;
            Subject.OnNext(new ViewFilter(filter, current.SourceFilter));
        }

        public void UpdateSourceFilter(Filter filter)
        {
            var current = Subject.Value;
            Subject.OnNext(new ViewFilter(current.Filter, filter));
        }
    }
}
