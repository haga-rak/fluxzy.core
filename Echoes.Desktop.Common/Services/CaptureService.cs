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

        public void Start()
        {
            if (_startupSetting == null || _proxyInstance != null)
                return;

            _proxyInstance = new Proxy(_startupSetting, new CertificateProvider(
                _startupSetting, new InMemoryCertificateCache()), OnNewExchange);

            var captureSession = Locator.Current.GetRequiredService<CaptureSession>();

            captureSession.Started = true; 

            _captureSessionSubject.OnNext(captureSession);

            _proxyInstance.Run();
        }

        private Task OnNewExchange(Exchange arg, ProxyExecutionContext proxyExecutionContext)
        {
            _captureSession.AddExchange(arg, proxyExecutionContext.SessionId);

            if (_captureSession != null)
                _captureSessionSubject.OnNext(_captureSession);

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (_proxyInstance == null)
                return Task.CompletedTask;

            if (_captureSession != null)
                _captureSession.Started = false; 

            try
            {
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