using System.Collections.Generic;
using System.Linq;
using Echoes.Core;
using Newtonsoft.Json;

namespace Echoes
{
    public class EchoesArchive
    {
        private EchoesArchive()
        {

        }

        [JsonConstructor]
        internal EchoesArchive(List<HttpExchange> exchanges)
        {
            Exchanges = exchanges ?? new List<HttpExchange>();
        }
        
        public List<HttpExchange> Exchanges { get; private set; }

        public EchoesArchive Copy()
        {
            return new EchoesArchive()
            {
                Exchanges = Exchanges.ToList()
            };
        }
        
        public static EchoesArchive CreateEmptyArchive()
        {
            return new EchoesArchive(new List<HttpExchange>());
        }
        
    }
}