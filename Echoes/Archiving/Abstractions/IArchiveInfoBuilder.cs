namespace Echoes.Archiving.Abstractions
{
    internal interface IArchiveInfoBuilder
    {
        public static IArchiveInfoBuilder FromExchange { get; } = new ArchiveInfoBuilder(); 

        ExchangeInfo Build(Exchange exchange); 

        ConnectionInfo Build(Connection connection); 
    }

    public class ArchiveInfoBuilder : IArchiveInfoBuilder
    {
        public ExchangeInfo Build(Exchange exchange)
        {
            var result = new ExchangeInfo()
            {
                Id = exchange.Id,
                ConnectionId = exchange.Connection?.Id ?? 0,
                Metrics = exchange.Metrics, // TODO put a copy constructor ?
                ResponseHeader = exchange.Response?.Header,
                RequestHeader = exchange.Request.Header,
            };

            return result;
        }

        public ConnectionInfo Build(Connection connection)
        {
            return new ConnectionInfo()
            {
                Id = connection.Id
            }; 
        }
    }
}