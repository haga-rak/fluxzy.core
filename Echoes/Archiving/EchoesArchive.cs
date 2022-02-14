using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Echoes
{
    public class EchoesArchive
    {
        private EchoesArchive()
        {

        }

        [JsonConstructor]
        internal EchoesArchive(List<Exchange> exchanges)
        {
            Exchanges = exchanges ?? new List<Exchange>();
        }
        
        public List<Exchange> Exchanges { get; private set; }

        public EchoesArchive Copy()
        {
            return new EchoesArchive()
            {
                Exchanges = Exchanges.ToList()
            };
        }
        
        public static EchoesArchive CreateEmptyArchive()
        {
            return new EchoesArchive(new List<Exchange>());
        }
        
    }
}