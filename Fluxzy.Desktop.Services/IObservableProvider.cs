// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services
{
    public interface IObservableProvider<out T>
    {
        T?  Current { get;  }

        IObservable<T> Observable { get; }
    }
}