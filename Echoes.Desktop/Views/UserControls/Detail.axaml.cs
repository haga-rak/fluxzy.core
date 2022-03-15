using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.Common.Extensions;
using Echoes.Desktop.ViewModels;
using Splat;

namespace Echoes.Desktop.Views.UserControls
{
    public partial class Detail : UserControl
    {
        public Detail()
        {
            InitializeComponent();
            DataContext = Locator.Current.GetRequiredService<DetailViewModel>(); 
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
        }
    }
}
