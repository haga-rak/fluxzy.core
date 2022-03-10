using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.Common;
using Splat;

namespace Echoes.Desktop.Views.UserControls
{
    public partial class LeftMenu : UserControl
    {

        public LeftMenu()
        {
            InitializeComponent();

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
