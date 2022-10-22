// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Fluxzy.Desktop.Services
{
    public abstract class ObservableProvider<T>
    {
        protected abstract BehaviorSubject<T> Subject { get; }

        public virtual IObservable<T> ProvidedObservable => Subject;
    }
}