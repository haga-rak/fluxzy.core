// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class UiState
    {
        public UiState(FileState fileState, ProxyState proxyState,
            FluxzySettingsHolder settingsHolder, SystemProxyState systemProxyState, 
            ViewFilter viewFilter, List<ToolBarFilter> toolBarFilters, TemplateToolBarFilterModel templateToolBarFilterModel) 
        {
            FileState = fileState;
            ProxyState = proxyState;
            SettingsHolder = settingsHolder;
            SystemProxyState = systemProxyState;
            ViewFilter = viewFilter;
            ToolBarFilters = toolBarFilters;
            TemplateToolBarFilterModel = templateToolBarFilterModel;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public FileState FileState { get;  }

        public ProxyState ProxyState { get; }

        public SystemProxyState SystemProxyState { get; }

        public ViewFilter ViewFilter { get; }

        public TemplateToolBarFilterModel TemplateToolBarFilterModel { get; }

        public List<ToolBarFilter> ToolBarFilters { get; }

        public FluxzySettingsHolder SettingsHolder { get;  }
        
    }
}