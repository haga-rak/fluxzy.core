// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Echoes.Clients;

namespace Echoes.Desktop.Common.Models
{
    public class CaptureSession
    {
        public void AddExchange(Exchange exchange, string sessionId)
        {
            Count++;

            lock (Items)
            {
                Items.Add(new ExchangeViewModel(exchange, sessionId));
            }
        }

        public ObservableCollection<ExchangeViewModel> Items { get;  } = new(); 

        public int Count { get; set; }

        public bool Started { get; set; }
    }
}