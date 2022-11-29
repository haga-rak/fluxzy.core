// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Formatters;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Extensions;
using Fluxzy.Readers;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;
using Fluxzy.Utils;

namespace Fluxzy.Desktop.Services
{
    public class ContextMenuActionProvider
    {
        private readonly IArchiveReaderProvider _archiveReaderProvider;
        private readonly ContextMenuFilterProvider _contextMenuFilterProvider;
        private readonly IObservable<ViewFilter> _viewFilterObservable;

        public ContextMenuActionProvider(
            IArchiveReaderProvider archiveReaderProvider, 
            ContextMenuFilterProvider contextMenuFilterProvider, 
            IObservable<ViewFilter> viewFilterObservable)
        {
            _archiveReaderProvider = archiveReaderProvider;
            _contextMenuFilterProvider = contextMenuFilterProvider;
            _viewFilterObservable = viewFilterObservable;
        }

        public async Task<ImmutableList<ContextMenuAction>?> GetActions(int exchangeId)
        {
            var actions = new List<ContextMenuAction>();
            var archiveReader = (await _archiveReaderProvider.Get())!;

            var exchange = archiveReader.ReadExchange(exchangeId);

            if (exchange == null)
                return ImmutableList.Create<ContextMenuAction>(); 

            actions.Add(ContextMenuAction.CreateInstance("delete", "Delete exchange"));

            // Adding source (agent) filters 

            if (exchange.Agent != null)
            {
                var viewFilter = await _viewFilterObservable.FirstAsync();

                if (!(viewFilter.SourceFilter is AgentFilter agentFilter)
                    || agentFilter.Agent?.Id != exchange.Agent.Id)
                {
                    var appliedAgentFilter = new AgentFilter(exchange.Agent);
                    actions.Add(ContextMenuAction.CreateFromSourceFilter(appliedAgentFilter));
                    actions.Add(ContextMenuAction.GetDivider());
                }
            }

            // Adding other filters

            var filterActions = _contextMenuFilterProvider.GetFilters(exchange, archiveReader).ToList();

            if (filterActions.Any()) {

                actions.Add(ContextMenuAction.GetDivider());
                actions.AddRange(filterActions.Select(f => ContextMenuAction.CreateInstance(f)));
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
                yield return ContextMenuAction.CreateInstance("download-request-body", "Save request body"); 

            if (archiveReader.HasResponseBody(exchange.Id))
                yield return ContextMenuAction.CreateInstance("download-response-body", "Save response body"); 
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
                    Description = $"Subdomain : «*.{subDomain}»"
                };
            }

            if (Uri.TryCreate(exchange.FullUrl, UriKind.Absolute,  out var absoluteUri) && absoluteUri.AbsolutePath.Length > 3) {
                yield return new PathFilter(absoluteUri.AbsolutePath, StringSelectorOperation.Contains); 
            }

            if (exchange.ResponseHeader != null) {
                var contentTypeHeader = exchange.GetResponseHeaderValue("content-type");

                if (contentTypeHeader != null) {
                    yield return new ResponseHeaderFilter(contentTypeHeader, "Content-Type"); 
                }
            }

            if (!string.IsNullOrWhiteSpace(exchange.Comment)) {
                yield return new CommentSearchFilter(exchange.Comment!) {
                    Description = $"Comment contains  «{exchange.Comment}»"
                };

                yield return new HasCommentFilter(); 
            }

            if (exchange.Tags.Any()) {
                yield return new HasTagFilter();

                foreach (var tag in exchange.Tags.Take(5)) {
                    yield return new TagContainsFilter(tag) {
                        Description = $"Has tag «{tag.Value}»"
                    }; 
                }

            }
        }
    }

    public class ContextMenuAction
    {
        private ContextMenuAction()
        {

        }

        private ContextMenuAction(string ? id, string? label)
        {
            Id = id; 
            Label = label;
            IsDivider = false;

        }

        public static ContextMenuAction CreateInstance(string? id, string? label)
        {
            return new ContextMenuAction(id, label);
        }

        private ContextMenuAction(Filter filter)
        {
            Id = filter.Identifier.ToString(); 
            Label = $"Filter : “{filter.FriendlyName}”";
            IsDivider = false;
            Filter = filter;
        }

        public static ContextMenuAction CreateInstance(Filter filter)
        {
            return new ContextMenuAction(filter);
        }

        public static ContextMenuAction CreateFromSourceFilter(AgentFilter filter)
        {
            var result = new ContextMenuAction(filter.Identifier.ToString(),
                $"Source : “{filter.Agent!.FriendlyName}”")
            {
                SourceFilter = filter
            };

            return result; 
        }
        
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

        public Filter ? SourceFilter { get; init;  }

        public static ContextMenuAction GetDivider() => new(null, null, true);




    }
}