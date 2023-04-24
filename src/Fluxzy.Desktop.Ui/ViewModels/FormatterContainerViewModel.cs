// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Formatters;

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FormatterContainerViewModel
    {
        public FormatterContainerViewModel(
            List<FormattingResult> requests, List<FormattingResult> responses, ExchangeContextInfo contextInfo)
        {
            Requests = requests;
            Responses = responses;
            ContextInfo = contextInfo;
        }

        public List<FormattingResult> Requests { get; }

        public List<FormattingResult> Responses { get; }

        public ExchangeContextInfo ContextInfo { get; }
    }
}