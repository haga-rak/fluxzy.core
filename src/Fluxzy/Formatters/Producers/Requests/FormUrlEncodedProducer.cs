// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class FormUrlEncodedProducer : IFormattingProducer<FormUrlEncodedResult>
    {
        public string ResultTitle => "Form data";

        public FormUrlEncodedResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (context.RequestBodyText == null)
                return null;

            var headerPresent =
                exchangeInfo.GetRequestHeaders().Any(h =>
                    h.Name.Span.Equals("Content-type", StringComparison.OrdinalIgnoreCase)
                    && h.Value.Span.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase));

            if (!headerPresent)
                return null;

            var res = HttpUtility.ParseQueryString(context.RequestBodyText);

            var items = res.AllKeys
                           .Where(k => k != null)
                           .SelectMany(k =>
                               res.GetValues(k)?
                                   .Select(v => new FormUrlEncodedItem(k!, v)) ?? new List<FormUrlEncodedItem>())
                           .ToList();

            if (!items.Any())
                return null;

            return new FormUrlEncodedResult(ResultTitle, items);
        }
    }

    public class FormUrlEncodedResult : FormattingResult
    {
        public FormUrlEncodedResult(string title, IEnumerable<FormUrlEncodedItem> items)
            : base(title)
        {
            Items = items.ToList();
        }

        public IReadOnlyCollection<FormUrlEncodedItem> Items { get; }
    }

    public class FormUrlEncodedItem
    {
        public FormUrlEncodedItem(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }
    }
}
