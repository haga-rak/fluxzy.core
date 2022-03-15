// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Reactive.Linq;
using Echoes.Clients;
using Echoes.Desktop.Common.Extensions;
using Echoes.Desktop.Common.Services;
using ReactiveUI;
using Splat;

namespace Echoes.Desktop.Common.Models
{
    public class ExchangeViewModel : ReactiveObject
    {
        public ExchangeViewModel(Exchange exchange, string sessionId, UiService uiService)
        {
            SessionId = sessionId;
            Id = exchange.Id;
            Method = exchange.Request.Header.Method.ToString();
            Url =
                new Uri($"{exchange.Request.Header.Scheme}://{exchange.Request.Header.Authority}{exchange.Request.Header.Path}")
                    .ToString();
            Protocol = exchange.HttpVersion;

            if (exchange.Response?.Header != null)
            {
                StatusCode = exchange.Response?.Header.StatusCode;
            }

            Done = exchange.Complete.IsCompleted;

            Selected = uiService.CurrentSelectedIds.Select(s => s.Contains(FullId));
        }


        public int Id { get; set; }

        public string SessionId { get; set; }

        public string FullId => $"{SessionId}_{Id}";

        public string Method { get; set; }

        public string Url { get; set; }

        public string Protocol { get; set; }

        public int? StatusCode { get; set; }

        public string FormattedSize { get; set; }

        public bool Done { get; set; }

        public IObservable<bool> Selected { get; }
    }
}