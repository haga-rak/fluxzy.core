// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FileOpeningViewModel
    {
        public FileOpeningViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }
    }
}
