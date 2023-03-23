// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core.Breakpoints;

namespace Fluxzy.Desktop.Services.BreakPoints
{
    public class BreakPointWatcher : ObservableProvider<BreakPointState>
    {
        private readonly BehaviorSubject<bool> _breakPointChangedWatcher = new(true);

        public BreakPointWatcher(ProxyControl proxyControl)
        {
            var breakPointManagerObservable =
                proxyControl.InternalProxy
                            .Where(s => s != null)
                            .Select(s => s!.ExecutionContext.BreakPointManager);

            breakPointManagerObservable.Do(
                OnNewBreakPointManager).Subscribe();

            var observableState =
                proxyControl.InternalProxy
                            .Where(s => s != null)
                            .CombineLatest(_breakPointChangedWatcher.AsObservable())
                            .Select(s => s.First)
                            .Select(s => s!.ExecutionContext.BreakPointManager.GetState());

            observableState.Do(s => Subject.OnNext(s)).Subscribe();
        }

        public BreakPointManager? BreakPointManager { get; private set; }

        protected override BehaviorSubject<BreakPointState> Subject { get; } = new(BreakPointState.EmptyEntries);

        private void OnNewBreakPointManager(BreakPointManager b)
        {
            if (BreakPointManager == b)
                return;

            if (BreakPointManager != null)
                BreakPointManager.OnContextUpdated -= ContextUpdated;

            BreakPointManager = b;

            BreakPointManager.OnContextUpdated += ContextUpdated;
        }

        public void ClearAllDone()
        {
            BreakPointManager?.ClearAllDone();

            if (BreakPointManager != null)
            {
                // Emit explicit change
                _breakPointChangedWatcher.OnNext(true);

            }
        }

        private void ContextUpdated(object sender, OnContextUpdatedArgs args)
        {
            // Break point state has changed 
            _breakPointChangedWatcher.OnNext(true);
        }
    }
}
