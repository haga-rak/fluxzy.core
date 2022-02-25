namespace Echoes.Archiving.Abstractions
{
    internal interface IArchiveInfoBuilder
    {
        ExchangeInfo Build(Exchange exchange); 
    }

    public class ArchiveInfoBuilder : IArchiveInfoBuilder
    {
        public ExchangeInfo Build(Exchange exchange)
        {
            var result = new ExchangeInfo()
            {
                Id = exchange.Id,
                ConnectionId = exchange.Connection?.Id ?? 0,
                Metrics = exchange.Metrics, // TODO put a copy constructor 
                ResponseHeader = exchange.Response?.Header,
                RequestHeader = exchange.Request.Header,
            };

            return result;
        }
    }
}