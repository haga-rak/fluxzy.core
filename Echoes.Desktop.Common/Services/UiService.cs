// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Echoes.Desktop.Common.Models;

namespace Echoes.Desktop.Common.Services
{
    public class UiService
    {
        private readonly BehaviorSubject<HashSet<string>> _currentSelection = new(new HashSet<string>());
        private HashSet<string> _latest;

        public UiService(CaptureService captureService)
        {
            _currentSelection.Subscribe(t => _latest = t);

            CurrentSelectedIds = _currentSelection.AsObservable();

            Selected = _currentSelection
                .CombineLatest(captureService.CaptureSession)
                .Select(s => s.Second.Items.Where(i => s.First.Contains(i.FullId)).ToList()); 
        }

        public IObservable<List<ExchangeViewModel>> Selected { get; }

        public IObservable<HashSet<string>> CurrentSelectedIds { get; }

        public void Reset()
        {
            _currentSelection.OnNext(new HashSet<string>());
        }
        

        public void Add(string fullId)
        {
            if (_latest.Add(fullId))
                _currentSelection.OnNext(_latest);
        }

        public void Set(string fullId)
        {
            _latest.Clear();
            _latest.Add(fullId);

           _currentSelection.OnNext(_latest);
        }

        public void Remove(string fullId)
        {
            if (_latest.Remove(fullId))
                _currentSelection.OnNext(_latest);
        }

    }
}