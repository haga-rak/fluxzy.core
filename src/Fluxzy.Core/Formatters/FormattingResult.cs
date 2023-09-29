// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Formatters
{
    public abstract class FormattingResult
    {
        protected FormattingResult(string title)
        {
            Title = title;
        }

        public string Title { get; }

        public string? ErrorMessage { get; set; }

        public string Type => GetType().Name;
    }
}
