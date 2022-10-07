// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Screeners;

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FormatterContainerViewModel
    {
        public FormatterContainerViewModel(List<FormattingResult> requests, List<FormattingResult> responses)
        {
            Requests = requests;
            Responses = responses;
        }

        public List<FormattingResult> Requests { get; }

        public List<FormattingResult> Responses { get; }
    }

    /// <summary>
    /// This is because System.Text.Json does not support serializing subclasses in .NET 6
    /// </summary>
    public class FormatterContainerViewModelGeneric
    {
        public FormatterContainerViewModelGeneric(FormatterContainerViewModel original)
        {
            Requests = original.Requests.OfType<object>().ToList();
            Responses = original.Responses.OfType<object>().ToList();
        }

        public List<object> Requests { get; }

        public List<object> Responses { get; }
    }
}