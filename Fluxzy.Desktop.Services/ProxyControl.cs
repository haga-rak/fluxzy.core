﻿using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Core;
using Fluxzy.Desktop.Services.Hubs;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Writers;
using Microsoft.AspNetCore.SignalR;

namespace Fluxzy.Desktop.Services
{
    public class ProxyControl : ObservableProvider<ProxyState>
    {
        private readonly ForwardMessageManager _forwardMessageManager;
        private readonly FilteredExchangeManager _filteredExchangeManager;
        private Proxy?  _proxy;
        private readonly BehaviorSubject<ProxyState> _internalSubject;
        private ViewFilter?  _viewFilter;
        private readonly Subject<RealtimeArchiveWriter?> _writerSubject = new(); 

        public ProxyControl(
            IObservable<FluxzySettingsHolder> fluxzySettingHolderObservable,
            IObservable<FileContentOperationManager> contentObservable,
            IObservable<ViewFilter> viewFilter,
             ForwardMessageManager forwardMessageManager,
            FilteredExchangeManager filteredExchangeManager)
        {
            _forwardMessageManager = forwardMessageManager;
            _filteredExchangeManager = filteredExchangeManager;

            _internalSubject = new BehaviorSubject<ProxyState>(new ProxyState()
            {
                OnError = true, 
                Message = "Not started yet" 
            });
            
            fluxzySettingHolderObservable
                .CombineLatest(
                    contentObservable
                        .DistinctUntilChanged(t => t.State.Identifier) // this will avoid unecessary refresh  
                    ) 
                .Select(stateAndSetting =>
                    System.Reactive.Linq.Observable.Create<ProxyState>(
                        async (observer, _) =>
                        {
                            var setting = stateAndSetting.First.StartupSetting;

                            var trunkState = await stateAndSetting.Second.Observable.FirstAsync();
                            
                            setting.ArchivingPolicy = 
                                ArchivingPolicy.CreateFromDirectory(
                                    stateAndSetting.Second.State.WorkingDirectory    
                                    );

                            var proxyState = await ReloadProxy(
                                setting, stateAndSetting.Second, 
                                trunkState.MaxConnectionId, trunkState.MaxExchangeId);

                            observer.OnNext(proxyState);
                            observer.OnCompleted();
                        }))
                .Switch()
                .Do(proxyState => 
                    _internalSubject.OnNext(proxyState)).Subscribe();

            Subject = _internalSubject;
            WriterObservable = _writerSubject.AsObservable();


            viewFilter
                .Do((v => _viewFilter = v))
                .Subscribe();

        }

        public IObservable<RealtimeArchiveWriter?> WriterObservable { get; } 

        private async Task<ProxyState> ReloadProxy(
            FluxzySetting fluxzySetting, 
            FileContentOperationManager currentContentOperationManager, int maxConnectionId, int maxExchangeId)
        {
            if (_proxy != null)
            {
                await _proxy.DisposeAsync();
                _proxy = null;
            }

            IEnumerable<IPEndPoint> endPoints;

            try
            {
                _proxy = new Proxy(fluxzySetting,
                    new CertificateProvider(fluxzySetting,
                        new InMemoryCertificateCache()));

                _proxy.IdProvider.SetNextConnectionId(maxConnectionId);
                _proxy.IdProvider.SetNextExchangeId(maxExchangeId);

                _writerSubject.OnNext(_proxy.Writer);

                _proxy.Writer.ExchangeUpdated += delegate(object? sender, ExchangeUpdateEventArgs args)
                {
                    currentContentOperationManager.AddOrUpdate(args.ExchangeInfo);

                    // Filter should be applied here 

                    if (_viewFilter?.Filter == null || _viewFilter.Filter.Apply(null, args.ExchangeInfo))
                    {
                        _forwardMessageManager.Send(args.ExchangeInfo);
                    }

                    _filteredExchangeManager.OnExchangeAdded(args.ExchangeInfo);
                };

                _proxy.Writer.ConnectionUpdated += delegate(object? sender, ConnectionUpdateEventArgs args)
                {
                    currentContentOperationManager.AddOrUpdate(args.Connection);
                    _forwardMessageManager.Send(args.Connection);
                };

                endPoints = _proxy.Run();
            }
            catch (Exception ex)
            {
                if (_proxy != null) {

                    _writerSubject.OnNext(null);

                    await _proxy.DisposeAsync();
                }

                return new ProxyState()
                {
                    OnError = true,
                    Message = ex.Message
                }; 
            }

            return GetProxyState(endPoints);
        }

        private ProxyState GetProxyState(IEnumerable<IPEndPoint> endPoints)
        {
            return new ProxyState()
            {
                BoundConnections = endPoints
                                   .Select(b => new ProxyEndPoint(b.Address.ToString(), b.Port))
                                   .ToList() ?? new List<ProxyEndPoint>()
            }; 
        }

        protected override BehaviorSubject<ProxyState> Subject { get; }
    
    }
}