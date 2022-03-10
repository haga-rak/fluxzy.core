using Avalonia.Controls;
using Echoes.Desktop.ViewModels;
using Splat;

namespace Echoes.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = Locator.Current.GetService<MainWindowViewModel>();
        }
    }
}
