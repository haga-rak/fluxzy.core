// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services
{
    public class ActiveViewFilterManager : ObservableProvider<ViewFilter>
    {
        protected override BehaviorSubject<ViewFilter> Subject { get; } = new(new ViewFilter(AnyFilter.Default));

        public void Update(ViewFilter filter)
        {
            Subject.OnNext(filter);
        }

        public ViewFilter Current => Subject.Value;
    }
}