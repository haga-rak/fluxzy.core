// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Screeners
{
    public abstract class FormattingResult
    {
        protected FormattingResult(string title)
        {
            Title = title;
        }

        public string Title {  get; }

        public string ? ErrorMessage { get; set; }

        public string Type => GetType().Name;
    }
}