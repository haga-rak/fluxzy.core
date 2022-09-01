namespace Fluxzy.Desktop.Services.Models
{
    public class TrunkState
    {
        public TrunkState(List<ExchangeInfo> exchanges, List<ConnectionInfo> connections)
        {
            Exchanges = exchanges;
            Connections = connections;
        }

        public List<ExchangeInfo> Exchanges { get; }

        public List<ConnectionInfo> Connections { get; }

        /// <summary>
        /// Used at client level 
        /// </summary>
        public Dictionary<int, ExchangeInfo> ExchangeIndex { get; } = new();


        public static TrunkState Empty()
        {
            return new TrunkState(new(), new()); 
        }
    }
}