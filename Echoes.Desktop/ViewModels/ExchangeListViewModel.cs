// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Echoes.Desktop.Common.Extensions;
using Echoes.Desktop.Common.Models;
using Echoes.Desktop.Common.Services;
using ReactiveUI;
using Splat;

namespace Echoes.Desktop.ViewModels
{
    public class ExchangeListViewModel : ViewModelBase
    {
        //public ExchangeListViewModel()
        //{
        //     Locator.Current.GetRequiredService<CaptureService>()
        //        .CaptureSession.Select(l => l.Items)
        //        .Subscribe(t =>
        //        {
        //            Items = t;
        //            this.RaisePropertyChanged(nameof(Items));
        //        });
        //}

        //public ObservableCollection<ExchangeViewModel> Items { get; set; }

        public ExchangeListViewModel()
        {
             Locator.Current.GetRequiredService<CaptureService>()
                .CaptureSession.Select(l => l.Items)
                .Subscribe(t =>
                {
                    Items = t;
                    this.RaisePropertyChanged(nameof(Items));
                });
            
        }

        public ObservableCollection<ExchangeViewModel> Items { get; set; }
    }
}