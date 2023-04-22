// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Immutable;
using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Formatters;
using Fluxzy.Readers;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

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

            var permanentFilter = false;

            if (exchange.Agent != null) {
                var viewFilter = await _viewFilterObservable.FirstAsync();

                if (!(viewFilter.SourceFilter is AgentFilter agentFilter)
                    || agentFilter.Agent?.Id != exchange.Agent.Id) {
                    var appliedAgentFilter = new AgentFilter(exchange.Agent);
                    actions.Add(ContextMenuAction.CreateFromSourceFilter(appliedAgentFilter));
                    permanentFilter = true;
                }
            }

            if (exchange.ConnectionId > 0)
                actions.Add(ContextMenuAction.CreateInstance(new ConnectionFilter(exchange.ConnectionId)));

            if (permanentFilter)
                actions.Add(ContextMenuAction.GetDivider());

            // Adding other filters

            var filterActions = _contextMenuFilterProvider.GetFilters(exchange, archiveReader).ToList();

            if (filterActions.Any()) {
                actions.Add(ContextMenuAction.GetDivider());
                actions.AddRange(filterActions.Select(f => ContextMenuAction.CreateInstance(f)));
            }

            var downloadActions = GetDownloadActions(exchange, archiveReader).ToList();

            if (downloadActions.Any()) {
                actions.Add(ContextMenuAction.GetDivider());
                actions.AddRange(downloadActions);
            }


            actions.Add(ContextMenuAction.GetDivider());
            actions.Add(ContextMenuAction.CreateInstance("replay", "Replay request"));
            actions.Add(ContextMenuAction.CreateInstance("replay-live-edit", "Replay request and live edit"));

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

    public class ContextMenuAction
    {
        private ContextMenuAction()
        {
        }

        private ContextMenuAction(string? id, string? label)
        {
            Id = id;
            Label = label;
            IsDivider = false;
        }

        private ContextMenuAction(Filter filter)
        {
            Id = filter.Identifier.ToString();
            Label = $"Filter : “{filter.FriendlyName}”";
            IsDivider = false;
            Filter = filter;
        }

        public ContextMenuAction(string? id, string? label, bool isDivider)
        {
            Id = id;
            Label = label;
            IsDivider = isDivider;
        }

        public string? Id { get; }

        public string? Label { get; }

        public bool IsDivider { get; }

        public Filter? Filter { get; set; }

        public Filter? SourceFilter { get; init; }

        public static ContextMenuAction CreateInstance(string? id, string? label)
        {
            return new ContextMenuAction(id, label);
        }

        public static ContextMenuAction CreateInstance(Filter filter)
        {
            return new ContextMenuAction(filter);
        }

        public static ContextMenuAction CreateFromSourceFilter(AgentFilter filter)
        {
            var result = new ContextMenuAction(filter.Identifier.ToString(),
                $"Source : “{filter.Agent!.FriendlyName}”") {
                SourceFilter = filter
            };

            return result;
        }

        public static ContextMenuAction GetDivider()
        {
            return new ContextMenuAction(null, null, true);
        }
    }
}
