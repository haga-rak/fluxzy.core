using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.Common;
using Splat;
using System;
using Avalonia.Interactivity;
using Echoes.Desktop.Common.Extensions;
using Echoes.Desktop.Common.Services;
using Echoes.Desktop.ViewModels;

namespace Echoes.Desktop.Views.UserControls
{
    public partial class TopMenu : UserControl
    {
        private readonly CaptureService _captureService;

        public TopMenu()
        {
            InitializeComponent();
            DataContext = Locator.Current.GetService<TopMenuViewModel>();
            _captureService = Locator.Current.GetRequiredService<CaptureService>();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void StartClick(object? sender, RoutedEventArgs e)
        {
            _captureService.Start();
        }

        private async void HaltClick(object? sender, RoutedEventArgs e)
        {
            await _captureService.Stop(); 
        }
    }
}
