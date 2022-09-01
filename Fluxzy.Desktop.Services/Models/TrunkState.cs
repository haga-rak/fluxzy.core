namespace Fluxzy.Desktop.Services.Models
{
    public class TrunkState
    {
        public TrunkState(List<ExchangeContainer> exchanges, List<ConnectionContainer> connections)
        {
            Exchanges = exchanges;
            Connections = connections;
        }

        public List<ExchangeContainer> Exchanges { get; }

        public List<ConnectionContainer> Connections { get; }

        /// <summary>
        /// Used at client level 
        /// </summary>
        public Dictionary<int, ExchangeContainer> ExchangeIndex { get; } = new();

        public static TrunkState Empty()
        {
            return new TrunkState(new(), new()); 
        }
    }
}