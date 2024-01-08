// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class QueryStringProducer : IFormattingProducer<QueryStringResult>
    {
        public string ResultTitle => "Query string";

        public QueryStringResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            var url = exchangeInfo.FullUrl;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            var items = HttpHelper.GetQueryStrings(uri);

            return !items.Any() ? null : new QueryStringResult(ResultTitle, items);
        }
    }

    public class QueryStringResult : FormattingResult
    {
        public QueryStringResult(string title, List<QueryStringItem> items)
            : base(title)
        {
            Items = items;
        }

        public List<QueryStringItem> Items { get; }
    }

    public class QueryStringItem
    {
        public QueryStringItem(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
