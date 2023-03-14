// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core.Breakpoints;
using Fluxzy.Desktop.Services.Ui;
using Fluxzy.Rules;

namespace Fluxzy.Desktop.Services.Models
{
    public class UiState
    {
        public UiState(
            FileState fileState, ProxyState proxyState,
            FluxzySettingsHolder settingsHolder, SystemProxyState systemProxyState,
            ViewFilter viewFilter, List<ToolBarFilter> toolBarFilters,
            TemplateToolBarFilterModel templateToolBarFilterModel,
            List<Rule> activeRules, LastOpenFileState lastOpenFileState, BreakPointState breakPointState)
        {
            FileState = fileState;
            ProxyState = proxyState;
            SettingsHolder = settingsHolder;
            SystemProxyState = systemProxyState;
            ViewFilter = viewFilter;
            ToolBarFilters = toolBarFilters;
            TemplateToolBarFilterModel = templateToolBarFilterModel;
            ActiveRules = activeRules;
            LastOpenFileState = lastOpenFileState;
            BreakPointState = breakPointState;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public FileState FileState { get; }

        public ProxyState ProxyState { get; }

        public SystemProxyState SystemProxyState { get; }

        public ViewFilter ViewFilter { get; }

        public TemplateToolBarFilterModel TemplateToolBarFilterModel { get; }

        public List<Rule> ActiveRules { get; }

        public List<ToolBarFilter> ToolBarFilters { get; }

        public FluxzySettingsHolder SettingsHolder { get; }

        public LastOpenFileState LastOpenFileState { get; }

        public BreakPointState BreakPointState { get; }
    }
}
