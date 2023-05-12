// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FileSaveViewModel
    {
        public FileSaveViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }

        public FileSaveOption? FileSaveOption { get; set; }
    }
}
