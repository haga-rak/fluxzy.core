using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.Common.Extensions;
using Echoes.Desktop.ViewModels;
using Splat;

namespace Echoes.Desktop.Views.UserControls
{
    public partial class ExchangeListView : UserControl
    {
        public ExchangeListView()
        {
            InitializeComponent();
            DataContext = Locator.Current.GetRequiredService<ExchangeListViewModel>();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
