// Copyright © 2022 Haga Rakotoharivelo

using Echoes.Desktop.Common.Models;
using Echoes.Desktop.Common.Services;
using System;
using System.Reactive.Linq;

namespace Echoes.Desktop.ViewModels
{
    public class DetailViewModel : ViewModelBase
    {
        public DetailViewModel(UiService service)
        {
            ExchangeModel = service.SelectedItem;
        }

        public IObservable<ExchangeViewModel?> ExchangeModel { get; }

        public IObservable<bool> HasModel => 
            ExchangeModel.Select(s => s != null)
            .DefaultIfEmpty(false); 
    }
}