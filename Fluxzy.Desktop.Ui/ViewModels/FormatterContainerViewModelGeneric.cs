// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Formatters;

namespace Fluxzy.Desktop.Ui.ViewModels
{
    /// <summary>
    /// This is because System.Text.Json does not support serializing subclasses in .NET 6
    /// </summary>
    public class FormatterContainerViewModelGeneric
    {
        public FormatterContainerViewModelGeneric(FormatterContainerViewModel original, ExchangeContextInfo contextInfo)
        {
            ContextInfo = contextInfo;
            Requests = original.Requests.OfType<object>().ToList();
            Responses = original.Responses.OfType<object>().ToList();
        }

        public List<object> Requests { get; }

        public List<object> Responses { get; }

        public ExchangeContextInfo ContextInfo { get; }
    }
}
