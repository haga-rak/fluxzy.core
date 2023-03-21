// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Desktop.Services.BreakPoints
{
    public class BreakPointHandler
    {
        private readonly ActiveRuleManager _activeRuleManager;
        private readonly BreakPointWatcher _watcher;

        public BreakPointHandler(ActiveRuleManager activeRuleManager, BreakPointWatcher watcher)
        {
            _activeRuleManager = activeRuleManager;
            _watcher = watcher;
        }

        public void AddBreakPoint(Filter filter)
        {
            _activeRuleManager.AddInMemoryRule(new Rule(new BreakPointAction(), filter));

        }

        public void BreakAll()
        {
            _activeRuleManager.SetInMemoryRule(new Rule(new BreakPointAction(), AnyFilter.Default));
        }

        public void DeleteBreakPoint(Guid filterId)
        {
            _activeRuleManager.RemoveInMemoryRule(filterId);
        }

        public void DeleteAllBreakPoints()
        {
            _activeRuleManager.ClearInMemoryRules();
        }

        public List<Rule> GetActiveBreakPoints()
        {
            return _activeRuleManager.GetInMemoryRules().ToList();
        }

        public void ContinueAll()
        {
            var contexts = _watcher.BreakPointManager?.GetAllContext();

            if (contexts != null) {
                foreach (var context in contexts) {
                    context.ContinueUntilEnd();
                }
            }
        }

        public void ContinueExchangeUntilEnd(int exchangeId)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.ContinueUntilEnd();
        }

        public void ContinueExchangeOnce(int exchangeId)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.ContinueOnce();
        }

        public void SetEndPoint(int exchangeId, ConnectionSetupStepModel model)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.ConnectionSetupCompletion.SetValue(model);
        }

        public void ContinueEndPoint(int exchangeId)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.ConnectionSetupCompletion.SetValue(default);
        }


        public void SetRequest(int exchangeId, RequestSetupStepModel model)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.RequestHeaderCompletion.SetValue(model);
        }

        public void ContinueRequest(int exchangeId)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.RequestHeaderCompletion.SetValue(default);
        }

        public void SetResponse(int exchangeId, ResponseSetupStepModel model)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.ResponseHeaderCompletion.SetValue(model);
        }

        public void ContinueResponse(int exchangeId)
        {
            var breakPointManager = _watcher.BreakPointManager;

            if (breakPointManager == null)
                return;

            if (!breakPointManager.TryGet(exchangeId, out var context))
                return;

            context!.ResponseHeaderCompletion.SetValue(default);
        }
    }
}
