using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Echoes.Desktop.Common;

namespace Echoes.Desktop.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly CaptureService _captureService;

        public MainWindowViewModel(CaptureService captureService)
        {
            _captureService = captureService;

            TaskBarStatus = captureService.CaptureSession.Select(BuildTaskBarStatus); 

        }

        private string BuildTaskBarStatus(CaptureSession session)
        {
            var res = $"Capture {(session.Started ? "ON" : "OFF")} : {session.Count} items";
            return res; 
        }

        public string Greeting => "Welcome to Avalonia!";

        public IObservable<string> TaskBarStatus { get;  }
    }
}
