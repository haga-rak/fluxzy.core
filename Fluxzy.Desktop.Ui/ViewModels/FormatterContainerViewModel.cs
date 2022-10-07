// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Formatters;
using Fluxzy.Screeners;

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FormatterContainerViewModel
    {
        public FormatterContainerViewModel(List<FormattingResult> requests, List<FormattingResult> responses, ExchangeContextInfo contextInfo)
        {
            Requests = requests;
            Responses = responses;
            ContextInfo = contextInfo;
        }

        public List<FormattingResult> Requests { get; }

        public List<FormattingResult> Responses { get; }

        public ExchangeContextInfo ContextInfo { get;  }
    }

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