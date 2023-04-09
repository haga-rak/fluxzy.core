// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    public class ExchangeContainer : IEquatable<ExchangeContainer>
    {
        public ExchangeContainer(IExchangeLine exchangeInfo)
        {
            Id = exchangeInfo.Id;
            ExchangeInfo = exchangeInfo;
        }

        public int Id { get; }

        public IExchangeLine ExchangeInfo { get; set; }

        public bool Equals(ExchangeContainer? other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((ExchangeContainer) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

    public class ExchangeContainerSorter : IComparer<ExchangeContainer>
    {
        public int Compare(ExchangeContainer? x, ExchangeContainer? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (ReferenceEquals(null, y))
                return 1;

            if (ReferenceEquals(null, x))
                return -1;

            return x.Id.CompareTo(y.Id);
        }

        public static ExchangeContainerSorter IdSorter { get; } = new ExchangeContainerSorter(); 
    }
}
