using System;
using Newtonsoft.Json;

namespace Echoes
{
    public partial class Hpm : HttpMessage
    {
        [JsonConstructor]
        internal Hpm(Guid requestId)
        {
            RequestId = requestId; 
            Id = Guid.NewGuid();
        }

        internal bool CloseDownStreamConnection { get; set; }
    }
}