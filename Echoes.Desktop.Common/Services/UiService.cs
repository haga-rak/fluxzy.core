// Copyright © 2022 Haga Rakotoharivelo

#nullable enable
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
        private readonly CaptureService _captureService;
        private readonly BehaviorSubject<SortedSet<string>> _currentSelection = new(new SortedSet<string>());

        private SortedSet<string> _latest = new();

        public UiService(CaptureService captureService)
        {
            _captureService = captureService;
            _currentSelection.Subscribe(t => _latest = t);

            CurrentSelectedIds = _currentSelection.AsObservable();

            SelectedItems = _currentSelection
                .CombineLatest(captureService.CaptureSession)
                .Select(s => s.Second.Items.Where(i => s.First.Contains(i.FullId)).ToList());

            SelectedItem = SelectedItems.Select(s => s.Any() ? s.Last() : null); 
        }

        public IObservable<ExchangeViewModel ?> SelectedItem { get; }

        public IObservable<List<ExchangeViewModel>> SelectedItems { get; }

        public IObservable<SortedSet<string>> CurrentSelectedIds { get; }

        public void Reset()
        {
            _currentSelection.OnNext(new SortedSet<string>());
        }
        

        public void Add(string fullId)
        {
            if (!_latest.Add(fullId))
                _latest.Remove(fullId);

            _currentSelection.OnNext(_latest);
        }

        public void Set(string fullId)
        {
            _latest.Clear();
            _latest.Add(fullId);
            
            _currentSelection.OnNext(_latest);
        }

        public void SetUntil(string fullId)
        {
            if (!_latest.Any())
            {
                Add(fullId);
                return; 
            }

            var lastSelectedItem = _latest.Last(); 

            var initialList = _captureService.Session.Items.ToList(); 

            var indexOfClick =
                initialList.FindIndex(t => t.FullId == fullId); 

            var indexOfLatest =
                initialList.FindIndex(t => t.FullId == lastSelectedItem);

            if (indexOfClick == -1 || indexOfLatest == -1)
                return; 

            var start = indexOfClick < indexOfLatest ? indexOfClick  : indexOfLatest;
            var end = indexOfClick > indexOfLatest ? indexOfClick  : indexOfLatest;

            for (var i = start; i <= end; i++)
            {
                _latest.Add(initialList[i].FullId);
            }

            _currentSelection.OnNext(_latest);
        }

        public void Remove(string fullId)
        {
            
            if (_latest.Remove(fullId))
                _currentSelection.OnNext(_latest);
        }

    }
}