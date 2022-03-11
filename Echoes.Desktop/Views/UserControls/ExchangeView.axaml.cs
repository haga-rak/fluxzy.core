using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.Common.Extensions;
using Echoes.Desktop.Common.Models;
using Echoes.Desktop.Common.Services;
using Splat;

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

        private void OnPanelClick(object? sender, PointerReleasedEventArgs e)
        {
            var model = (ExchangeViewModel) DataContext!; 

            Console.WriteLine("click");

            Locator
                .Current.GetRequiredService<UiService>()
                .Set(model.FullId);
        }
    }
}
