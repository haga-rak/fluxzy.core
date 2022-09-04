namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FileOpeningViewModel
    {
        public FileOpeningViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; set; }
    }

    public class FileSaveViewModel
    {
        public FileSaveViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; set; }
    }
}
