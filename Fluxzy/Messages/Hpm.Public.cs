using System;
using Newtonsoft.Json;

namespace Echoes
{
    public partial class Hpm
    {
        [JsonProperty]
        public Guid RequestId { get; internal set; }

        [JsonProperty]
        public int StatusCode { get; internal set; } = -1;

        [JsonProperty]
        public DateTime? ServerConnected { get; internal set; }

        [JsonProperty]
        public DateTime? UpStreamStartSendingHeader { get; internal set; }

        [JsonProperty]
        public DateTime? UpStreamCompleteSendingHeader { get; internal set; }

        [JsonProperty]
        public DateTime? UpStreamStartSendingBody { get; internal set; }

        [JsonProperty]
        public DateTime? UpStreamCompleteSendingBody { get; internal set; }
    }
}
