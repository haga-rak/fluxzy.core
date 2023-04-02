// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FullUrlSearchViewModel
    {
        public FullUrlSearchViewModel(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get;  }
    }
}
