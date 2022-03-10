// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Reactive.Linq;
using Avalonia.Markup.Xaml;
using Echoes.Desktop.Common;
using Splat;

namespace Echoes.Desktop.ViewModels
{
    public class TopMenuViewModel : ViewModelBase
    {
        private readonly CaptureService _captureService;

        public TopMenuViewModel(CaptureService captureService)
        {
            _captureService = captureService;

            StartEnable = captureService.CaptureState.Select(s => s == CaptureStateType.Halted);
            StopEnable = captureService.CaptureState.Select(s => s == CaptureStateType.Running);

            captureService.CaptureState.Subscribe(t => CaptureState = t);
        }

        public IObservable<bool> StopEnable { get; }

        public IObservable<bool> StartEnable { get; }

        public CaptureStateType CaptureState { get; set; }
        
    }
}