// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Formatters;
using System.Collections.Immutable;
using Fluxzy.Extensions;
using Fluxzy.Readers;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Newtonsoft.Json;
using Fluxzy.Utils;

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

            // Adding filters 

            var filterActions = _contextMenuFilterProvider.GetFilters(exchange, archiveReader).ToList();

            if (filterActions.Any()) {

                actions.Add(ContextMenuAction.GetDivider());
                actions.AddRange(filterActions.Select(f => new ContextMenuAction(f)));
            }

            var downloadActions = GetDownloadActions(exchange, archiveReader).ToList();

            if (downloadActions.Any())
            {
                actions.Add(ContextMenuAction.GetDivider());
                actions.AddRange(downloadActions);
            }

            return actions.ToImmutableList(); 
        }

        private IEnumerable<ContextMenuAction> GetDownloadActions(ExchangeInfo exchange, IArchiveReader archiveReader)
        {
            if (archiveReader.HasRequestBody(exchange.Id))
                yield return new ContextMenuAction("download-request-body", "Save request body"); 

            if (archiveReader.HasResponseBody(exchange.Id))
                yield return new ContextMenuAction("download-response-body", "Save response body"); 

        }
    }

    public class ContextMenuFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ExchangeInfo exchange, IArchiveReader archiveReader)
        {
            // Filter by hostname 

            var authority = exchange.RequestHeader.Authority.ToString(); 

            yield return new HostFilter(authority, 
                StringSelectorOperation.Exact) {
                Description = $"Host : {authority}"
            };

            if (SubdomainUtility.TryGetSubDomain(authority, out var subDomain)) {

                yield return new HostFilter(subDomain!,
                    StringSelectorOperation.EndsWith)
                {
                    Description = $"Subdomain of : «*.{subDomain}»"
                };
            }

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
            Filter = filter;
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