// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services
{
    public interface IObservableProvider<T>
    {
        T?  Current { get;  }

        IObservable<T> Observable { get; }
    }

    public abstract class CurrentStateObservable<T> : IObservable<T>
    {
        private T? _current;
        private readonly List<IObserver<T>> _observers = new();

        protected void SetInitialValue(T value)
        {
            _current = value;
        }

        protected void ValueUpdated(T newUpdate)
        {
            _current = newUpdate;
            foreach (var observer in _observers.ToList())
            {
                observer.OnNext(_current); 
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (_observers)
                _observers.Add(observer);

            if (_current != null)
                observer.OnNext(_current);

            return new UnsubscribeDisposable<T>(_observers, observer); 
        }
    }


    internal class UnsubscribeDisposable<T> : IDisposable
    {
        private readonly List<IObserver<T>> _list;
        private readonly IObserver<T> _item;

        public UnsubscribeDisposable(List<IObserver<T>> _list, IObserver<T> item)
        {
            this._list = _list;
            _item = item;
        }

        public void Dispose()
        {
            lock (_list)
            {
                _list.Remove(_item);
            }
        }
    }
}