using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.Common.Models;

namespace Echoes.Desktop.Views.UserControls
{
    public partial class ExchangeView : UserControl
    {
        public ExchangeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private object _coco; 

        public object Coco
        {
            get
            {
                return _coco; 
            }
            set
            {
                _coco = value; 
            }
        }
    }
}
