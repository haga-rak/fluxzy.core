using System;
using System.Threading;
using Echoes.Core;
using Newtonsoft.Json;

namespace Echoes
{
    //public class HttpExchange
    //{
    //    private static int _index;

    //    [JsonConstructor]
    //    public HttpExchange(
    //        Hrm requestMessage,
    //        Hpm responseMessage, 
    //        IDownStreamConnection downStreamConnection, 
    //        IUpstreamConnection upstreamConnection)
    //    {
    //        Id = requestMessage?.Id ?? Guid.Empty;
    //        Index = Interlocked.Increment(ref _index);
    //        RequestMessage = requestMessage;
    //        ResponseMessage = responseMessage;

    //        if (downStreamConnection != null)
    //            DownStreamEndPointInfo = new EndPointInformation(downStreamConnection);

    //        if (upstreamConnection != null)
    //            UpStreamEndPointInfo = new EndPointInformation(upstreamConnection);
    //    }

    //    public HttpExchange(Hrm requestMessage, Hpm responseMessage, EndPointInformation downStreamEndPointInfo, EndPointInformation upStreamEndPointInfo)
    //    {
    //        Id = requestMessage?.Id ?? Guid.Empty;
    //        Index = Interlocked.Increment(ref _index);

    //        RequestMessage = requestMessage;
    //        ResponseMessage = responseMessage;
    //        DownStreamEndPointInfo = downStreamEndPointInfo;
    //        UpStreamEndPointInfo = upStreamEndPointInfo;
    //    }

    //    [JsonProperty]
    //    public int Index { get; internal set; }

    //    [JsonProperty]
    //    public Guid Id { get; internal set; }

    //    [JsonProperty]
    //    public Hrm RequestMessage { get; internal set; }

    //    [JsonProperty]
    //    public Hpm ResponseMessage { get; internal set; }

    //    [JsonProperty]
    //    public EndPointInformation DownStreamEndPointInfo { get; internal set; }

    //    [JsonProperty]
    //    public EndPointInformation UpStreamEndPointInfo { get; internal set; }
    //}


}