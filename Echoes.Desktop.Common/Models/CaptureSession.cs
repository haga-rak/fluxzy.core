// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicData;
using Echoes.Clients;
using Echoes.Desktop.Common.Services;

namespace Echoes.Desktop.Common.Models
{
    public class CaptureSession
    {
        public void AddExchange(Exchange exchange, string sessionId, UiService uiService)
        {
            var item = new ExchangeViewModel(exchange, sessionId, uiService);

            lock (Items)
            {
                Count++;
                Items.Add(item);
                IndexedItems[item.FullId] = Items.Count - 1;
            }
        }

        public void Update(Exchange exchange, string sessionId, UiService uiService)
        {
            var id = $"{sessionId}_{exchange.Id}";

            lock (Items)
            {
                Items[IndexedItems[id]] = new ExchangeViewModel(exchange, sessionId, uiService);
            }
        }

        private Dictionary<string, int> IndexedItems { get; } = new();

        public ObservableCollection<ExchangeViewModel> Items { get;  } = new(); 

        public int Count { get; set; }

        public bool Started { get; set; }
    }
}