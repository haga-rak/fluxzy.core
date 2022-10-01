// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers
{
    public class QueryStringProducer : IFormattingProducer<QueryStringResult>
    {
        public string ResultTitle => throw new NotImplementedException();

        public QueryStringResult? Build(ExchangeInfo exchangeInfo, IArchiveReader archiveReader)
        {
            var url = exchangeInfo.FullUrl;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            var res = HttpUtility.ParseQueryString(uri.Query);
            

            var items = res.AllKeys.SelectMany(k => res.GetValues((string)k)?.Select(v => new QueryStringItem(k, v)))
                           .Where(t => t != null)
                           .ToList();

            return !items.Any() ? null : new QueryStringResult(items);
        }
    }


    public class QueryStringResult : FormattingResult
    {
        public QueryStringResult(List<QueryStringItem> items) : base("Query string")
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