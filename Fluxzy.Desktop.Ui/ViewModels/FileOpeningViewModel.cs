namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class FileOpeningViewModel
    {
        public FileOpeningViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get;  }
    }

    public class FileSaveViewModel
    {
        public FileSaveViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get;  }
    }

    public class FileOpeningRequestViewModel
    {
        public FileOpeningRequestViewModel(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get;  }
    }

    public class ApplyTagViewModel
    {
        public Guid ? TagIdentifier { get; set; }

        public string ? TagName { get; set; }

        public List<int> ExchangeIds { get; set; }
    }
}
