namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FileOpeningRequestViewModel
    {
        public FileOpeningRequestViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get;  }
    }
}