using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.ViewModels;
using Echoes.Desktop.Views;
using Splat;

namespace Echoes.Desktop
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = Locator.Current.GetService<MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
