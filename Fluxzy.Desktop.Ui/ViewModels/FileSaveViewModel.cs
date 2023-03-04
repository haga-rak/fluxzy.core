namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FileSaveViewModel
    {
        public FileSaveViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get;  }
    }
}