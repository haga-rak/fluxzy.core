// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Readers;

namespace Fluxzy.Desktop.Services
{
    public class FileContentUpdateManager
    {
        private readonly ForwardMessageManager _forwardMessageManager;
        private readonly FilteredExchangeManager _filteredExchangeManager;
        private ViewFilter? _viewFilter;
        private FileContentOperationManager?  _currentContentOperationManager;

        public FileContentUpdateManager(ForwardMessageManager forwardMessageManager, 
            IObservable<ViewFilter> viewFilterObservable, FilteredExchangeManager filteredExchangeManager,
            IObservable<FileContentOperationManager> contentObservable)
        {
            _forwardMessageManager = forwardMessageManager;
            _filteredExchangeManager = filteredExchangeManager;

            viewFilterObservable
                .Do(t => this._viewFilter = t)
                .Subscribe();

            contentObservable
                .Do(c => _currentContentOperationManager = c)
                .Subscribe(); 
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
            {
                _forwardMessageManager.Send(exchangeInfo);
            }

            _filteredExchangeManager.OnExchangeAdded(exchangeInfo, archiveReader);
        }
    }
}