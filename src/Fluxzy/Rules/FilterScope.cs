// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;

namespace Fluxzy.Rules
{
    /// <summary>
    ///     The filter scope defines the minimal timing where a filter can be run.
    ///     These specific timing are :
    ///     - OnAuthorityReceived : This scope denotes the moment fluxzy is aware the destination authority.
    ///  In a regular proxy connection, tt will occur the moment where
    ///     fluxzy parsed
    ///     CONNECT request.
    ///     - RequestHeaderReceivedFromClient : The moment when the full request header is parsed
    ///     - RequestBodyReceivedFromClient :  This timing is trigger only after the request body is sent to the upstream due
    ///     to streaming
    ///     - ResponseHeaderReceivedFromRemote : The moment where fluxzy got the response header from the server
    ///     - ResponseBodyReceivedFromRemote :  (Aka complete) This timing is trigger only after the response body is sent to
    ///     the downstream due to streaming.
    ///     - OutOfScope : Indicates a filter that is not usable for live alteration. This kind of filter applies only for view
    ///     filters in Fluxzy Desktop
    /// </summary>
    public enum FilterScope
    {
        [Description("This scope denotes the moment fluxzy is aware the destination authority." +
                     " In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.")]
        OnAuthorityReceived,

        [Description("This scope occurs the moment fluxzy parsed the request header receiveid from client")]
        RequestHeaderReceivedFromClient,

        [Description("This scope occurs the moment fluxzy received fully the request body from the client. In a full" +
                     "streaming mode which is the default mode, this event occurs when the full body is already fully sent to the remote server. ")]
        RequestBodyReceivedFromClient,

        [Description("This scope occurs the moment fluxzy has done parsing the response header.")]
        ResponseHeaderReceivedFromRemote,

        [Description("This scope occurs the moment fluxzy received the the response body from the server. " +
                     "In a full streaming mode (which is the default mode), this event occurs the the full body is already sent to the client.")]
        ResponseBodyReceivedFromRemote,

        [Description("Means that the filter or action associated to this scope won't be trigger in the regular HTTP flow. This scope" +
                     " is applied only on view filter and internat actions.")]
        OutOfScope = 99999
    }
}
