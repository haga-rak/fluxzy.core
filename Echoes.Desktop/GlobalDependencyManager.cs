// Copyright © 2022 Haga Rakotoharivelo

using Echoes.Desktop.Common.Models;
using Echoes.Desktop.Common.Services;
using Echoes.Desktop.ViewModels;
using Echoes.Desktop.Views;
using Ninject;
using Splat.Ninject;

namespace Echoes.Desktop.Common
{
    public class GlobalDependencyManager
    {
        public static void SetupIoc()
        {
            var kernel = new StandardKernel();

            kernel.Bind<CaptureService>().ToSelf().InSingletonScope();
            kernel.Bind<UiService>().ToSelf().InSingletonScope();
            kernel.Bind<SettingHolder>().ToSelf().InSingletonScope();
            
            kernel.Bind<MainWindow>().ToSelf().InSingletonScope();

            kernel.Bind<TopMenuViewModel>().ToSelf();
            kernel.Bind<DetailViewModel>().ToSelf();
            kernel.Bind<MainWindowViewModel>().ToSelf();
            kernel.Bind<ExchangeListViewModel>().ToSelf();

            kernel.Bind<CaptureSession>().ToSelf();

            kernel.UseNinjectDependencyResolver();
        }
    }
}