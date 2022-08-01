using Newtonsoft.Json;

namespace Echoes
{
    public partial class Hrm : HttpMessage
    {
        [JsonConstructor]
        internal Hrm()
        {

        }

        [JsonProperty]
        internal string RawMethod { get; set; }
    }
}