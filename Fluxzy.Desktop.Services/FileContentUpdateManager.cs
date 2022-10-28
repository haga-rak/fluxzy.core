// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;

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
                .Do(c => this._currentContentOperationManager = c); 
        }

        public void AddOrUpdate(ConnectionInfo connectionInfo)
        {
            if (_currentContentOperationManager == null)
                return;

            _currentContentOperationManager.AddOrUpdate(connectionInfo);
            _forwardMessageManager.Send(connectionInfo);
        }


        // TODO : Move this role to another object 
        public void AddOrUpdate(ExchangeInfo exchangeInfo)
        {
            if (_currentContentOperationManager == null)
                return;

            _currentContentOperationManager.AddOrUpdate(exchangeInfo);
            
            if (_viewFilter?.Filter == null || _viewFilter.Filter.Apply(null, exchangeInfo, null))
            {
                _forwardMessageManager.Send(exchangeInfo);
            }

            _filteredExchangeManager.OnExchangeAdded(exchangeInfo);
        }
    }
}