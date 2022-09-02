// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Fluxzy.Desktop.Services
{
    public abstract class ObservableProvider<T>
    {
        public abstract BehaviorSubject<T> Subject { get; }

        public IObservable<T> Observable => Subject.AsObservable();
    }
}