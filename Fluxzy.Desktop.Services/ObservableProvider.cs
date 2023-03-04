// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Subjects;

namespace Fluxzy.Desktop.Services
{
    public abstract class ObservableProvider<T>
    {
        protected abstract BehaviorSubject<T> Subject { get; }

        public virtual IObservable<T> ProvidedObservable => Subject;
    }
}
