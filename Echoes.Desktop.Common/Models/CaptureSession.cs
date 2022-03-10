// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Desktop.Common
{
    public class CaptureSession
    {
        public void AddExchange(Exchange exchange)
        {
            Count++;

            lock (Items)
            {
                Items.Add(exchange);
            }
        }

        public List<Exchange> Items { get;  } = new(); 

        public int Count { get; set; }

        public bool Started { get; set; }
    }
}