// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters
{
    /// <summary>
    /// The filter scope defines the minimal timing where a filter can be run.
    /// These specific timing are :
    ///  - OnAuthorityReceived : The moment when fluxzy knows the destination authority. It will occur the moment where fluxzy parsed
    /// CONNECT request in a typical proxy communication
    ///  - RequestHeaderReceivedFromClient : The moment when the full request header is parsed
    ///  - RequestBodyReceivedFromClient :  This timing is trigger only after the request body is sent to the upstream due to streaming
    ///  - ResponseHeaderReceivedFromRemote : The moment where fluxzy got the response header from the server
    ///  - ResponseBodyReceivedFromRemote :  This timing is trigger only after the response body is sent to the downstream due to streaming
    ///  - OutOfScope : Indicates a filter that is not usable for live alteration. This kind of filter applies only for view filters in Fluxzy Desktop
    /// </summary>
    public enum FilterScope
    {
        OnAuthorityReceived,
        RequestHeaderReceivedFromClient,
        RequestBodyReceivedFromClient,
        ResponseHeaderReceivedFromRemote,
        ResponseBodyReceivedFromRemote,

        OutOfScope = 99999
    }
}