// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Models;
using Fluxzy.Readers;
using System.Reactive.Linq;

namespace Fluxzy.Desktop.Services
{
    public class FileContentUpdateManager
    {
        private readonly FilteredExchangeManager _filteredExchangeManager;
        private readonly ForwardMessageManager _forwardMessageManager;
        private FileContentOperationManager? _currentContentOperationManager;
        private ViewFilter? _viewFilter;

        public FileContentUpdateManager(
            ForwardMessageManager forwardMessageManager,
            IObservable<ViewFilter> viewFilterObservable, FilteredExchangeManager filteredExchangeManager,
            IObservable<FileContentOperationManager> contentObservable)
        {
            _forwardMessageManager = forwardMessageManager;
            _filteredExchangeManager = filteredExchangeManager;

            viewFilterObservable
                .Do(t => _viewFilter = t)
                .Subscribe();

            contentObservable
                .Do(c => _currentContentOperationManager = c)
                .Subscribe();
        }

        public void UpdateErrorCount(int errorCount)
        {
            if (_currentContentOperationManager == null)
                return;

            _currentContentOperationManager.UpdateErrorCount(errorCount);
            _forwardMessageManager.Send(new DownstreamCountUpdate(errorCount));
        }

        public void AddOrUpdate(ConnectionInfo connectionInfo)
        {
            if (_currentContentOperationManager == null)
                return;

            _currentContentOperationManager.AddOrUpdate(connectionInfo);
            _forwardMessageManager.Send(connectionInfo);
        }

        public void AddOrUpdate(ExchangeInfo exchangeInfo, IArchiveReader archiveReader)
        {
            if (_currentContentOperationManager == null)
                return;

            _currentContentOperationManager.AddOrUpdate(exchangeInfo);

            if (_viewFilter == null || _viewFilter.Apply(null, exchangeInfo, archiveReader))
                _forwardMessageManager.Send(exchangeInfo);

            _filteredExchangeManager.OnExchangeAdded(exchangeInfo, archiveReader);
        }
    }
}
