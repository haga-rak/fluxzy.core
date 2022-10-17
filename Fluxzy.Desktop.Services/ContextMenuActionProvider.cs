// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Formatters;
using System.Collections.Immutable;
using Fluxzy.Extensions;
using Fluxzy.Readers;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Newtonsoft.Json;

namespace Fluxzy.Desktop.Services
{
    public class ContextMenuActionProvider
    {
        private readonly IArchiveReaderProvider _archiveReaderProvider;
        private readonly ContextMenuFilterProvider _contextMenuFilterProvider;

        public ContextMenuActionProvider(IArchiveReaderProvider archiveReaderProvider, ContextMenuFilterProvider contextMenuFilterProvider)
        {
            _archiveReaderProvider = archiveReaderProvider;
            _contextMenuFilterProvider = contextMenuFilterProvider;
        }

        public async Task<ImmutableList<ContextMenuAction>?> GetActions(int exchangeId)
        {
            var actions = new List<ContextMenuAction>();
            var archiveReader = (await _archiveReaderProvider.Get())!;

            var exchange = archiveReader.ReadExchange(exchangeId);

            if (exchange == null)
                return ImmutableList.Create<ContextMenuAction>(); 

            actions.Add(new ContextMenuAction("delete", "Delete exchange"));
            actions.Add(ContextMenuAction.GetDivider());

            // Adding filters 

            var filterActions = _contextMenuFilterProvider.GetFilters(exchange, archiveReader).ToList();

            if (filterActions.Any()) {
                actions.AddRange(filterActions.Select(f => new ContextMenuAction(f)));
            }

            return actions.ToImmutableList(); 
        }
    }

    public class ContextMenuFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ExchangeInfo exchange, IArchiveReader archiveReader)
        {
            // Filter by hostname 

            yield return new HostFilter(exchange.RequestHeader.Authority.ToString(), 
                StringSelectorOperation.EndsWith); 

            if (Uri.TryCreate(exchange.FullUrl, UriKind.Absolute,  out var absoluteUri)) {
                yield return new PathFilter(absoluteUri.AbsolutePath, StringSelectorOperation.Contains); 
            }

            if (exchange.ResponseHeader != null) {
                var contentTypeHeader = exchange.GetResponseHeaderValue("content-type");

                if (contentTypeHeader != null) {
                    yield return new ResponseHeaderFilter(contentTypeHeader, "Content-Type"); 
                }
            }
        }
    }

    public class ContextMenuAction
    {
        public ContextMenuAction(string ? id, string? label)
        {
            Id = id; 
            Label = label;
            IsDivider = false;

        }
        public ContextMenuAction(Filter filter)
        {
            Id = filter.Identifier.ToString(); 
            Label = $"Filter : “{filter.FriendlyName}”";
            IsDivider = false;
        }

        [JsonConstructor]
        public ContextMenuAction(string ? id, string? label, bool isDivider)
        {
            Id = id; 
            Label = label;
            IsDivider = isDivider;
        }

        public string ? Id { get;  }

        public string ? Label { get;  }

        public bool IsDivider { get;  }

        public Filter ? Filter { get; set;  }

        public static ContextMenuAction GetDivider() => new(null, null, true);
    }
}