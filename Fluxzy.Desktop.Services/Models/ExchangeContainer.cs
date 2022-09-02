namespace Fluxzy.Desktop.Services.Models
{
    public class ExchangeContainer
    {
        public ExchangeContainer(ExchangeInfo exchangeInfo)
        {
            Id = exchangeInfo.Id; 
            ExchangeInfo = exchangeInfo;
        }

        public int Id { get;  }

        public ExchangeInfo ExchangeInfo { get; set;  }
    }
}