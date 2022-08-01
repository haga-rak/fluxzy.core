using System;
using Newtonsoft.Json;

namespace Echoes
{
    public partial class Hrm 
    {
        [JsonProperty]
        public string DestinationHost { get; internal set; }


        private HrmMethod? _method;

        [JsonIgnore]
        public HrmMethod Method
        {
            get
            {
                if (_method != null)
                    return _method.Value;

                if (string.IsNullOrWhiteSpace(RawMethod))
                    return (_method = HrmMethod.Unkown).Value;

                if (!Enum.TryParse<HrmMethod>(RawMethod, true, out var parsedMethod))
                    return (_method = HrmMethod.Unkown).Value;

                _method = (HrmMethod) parsedMethod;
                return _method.Value;
            }
        }

        [JsonProperty]
        public Uri Uri { get; internal set; }

        [JsonProperty]
        public bool IsTunnelConnectionRequested { get; internal set; }

        [JsonProperty]
        public DateTime? ClientConnected { get; internal set; }

        [JsonProperty]
        public DateTime? SslConnectionStart { get; internal set; }

        [JsonProperty]
        public DateTime? SslConnectionEnd { get; internal set; }

        [JsonProperty]
        public DateTime? DownStreamStartSendingHeader { get; internal set; }

        [JsonProperty]
        public DateTime? DownStreamCompleteSendingHeader { get; internal set; }

        [JsonProperty]
        public DateTime? SendingHeaderToUpStream { get; internal set; }

        [JsonProperty]
        public DateTime? HeaderSentToUpStream { get; internal set; }

        [JsonProperty]
        public DateTime? BodySentToUpStream { get; internal set; }
    }
}
