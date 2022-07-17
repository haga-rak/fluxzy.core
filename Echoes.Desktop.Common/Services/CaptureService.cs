// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Echoes.Clients;
using Echoes.Core;
using Echoes.Desktop.Common.Extensions;
using Echoes.Desktop.Common.Models;
using Splat;

namespace Echoes.Desktop.Common.Services
{
    public class CaptureService
    {
        private readonly SettingHolder _holder;
        
        private readonly BehaviorSubject<CaptureSession> _captureSessionSubject = new(new CaptureSession());

        private ProxyStartupSetting _startupSetting;
        private Proxy _proxyInstance;
        private CaptureSession  _captureSession;
        private UiService _uiService;

        public CaptureService(SettingHolder holder)
        {
            _holder = holder;
            _holder.GetStartupSetting().Subscribe(t => _startupSetting = t);
            _captureSessionSubject.Subscribe(t => _captureSession = t);

            CaptureSession.Subscribe();
            CaptureState.Subscribe();
        }

        public IObservable<CaptureSession> CaptureSession => _captureSessionSubject.AsObservable(); 

        public IObservable<CaptureStateType> CaptureState => _captureSessionSubject
            .Select(s => s.Started ? CaptureStateType.Running : CaptureStateType.Halted );

        public CaptureSession Session => _captureSession;

        public void Start()
        {
            if (_startupSetting == null || _proxyInstance != null)
                return;

            if (_uiService == null)
                _uiService = Locator.Current.GetRequiredService<UiService>();

            _proxyInstance = Proxy.Create(_startupSetting);

            _proxyInstance.BeforeRequest += ProxyInstanceOnBeforeRequest;
            _proxyInstance.BeforeResponse += ProxyInstanceOnBeforeResponse;

            var captureSession = Locator.Current.GetRequiredService<CaptureSession>();

            captureSession.Started = true; 

            _captureSessionSubject.OnNext(captureSession);

            _proxyInstance.Run();
        }

        private void ProxyInstanceOnBeforeResponse(object? sender, BeforeResponseEventArgs e)
        {
            lock (this)
            {
                _captureSession.Update(e.Exchange, e.ExecutionContext.SessionId, _uiService);
                _captureSessionSubject.OnNext(_captureSession);
            }
        }

        private void ProxyInstanceOnBeforeRequest(object? sender, BeforeRequestEventArgs e)
        {

            lock (this)
            {
                _captureSession.AddExchange(e.Exchange, e.ExecutionContext.SessionId, _uiService);
                _captureSessionSubject.OnNext(_captureSession);
            }
        }


        public Task Stop()
        {
            if (_proxyInstance == null)
                return Task.CompletedTask;

            if (_captureSession != null)
                _captureSession.Started = false; 

            try
            {
                _proxyInstance.BeforeRequest -= ProxyInstanceOnBeforeRequest;
                _proxyInstance.BeforeResponse -= ProxyInstanceOnBeforeResponse;

                _proxyInstance.Dispose();
            }
            finally
            {
                _proxyInstance = null;
                _captureSessionSubject.OnNext(_captureSession);
            }

            return Task.CompletedTask;
        }
    }
}