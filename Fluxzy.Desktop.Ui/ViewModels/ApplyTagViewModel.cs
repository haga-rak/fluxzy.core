namespace Fluxzy.Desktop.Ui.ViewModels
{
    public class ApplyTagViewModel
    {
        public Guid ? TagIdentifier { get; set; }

        public string ? TagName { get; set; }

        public List<int> ExchangeIds { get; set; }
    }
}