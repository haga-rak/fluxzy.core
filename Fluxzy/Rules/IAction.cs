﻿// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public class Rule
    {
        public ActionTiming Timing { get; set; }

        public FilterCollection Filter { get; set; } = new(); 

        public List<IAction> Actions { get; set; }

        public async Task Enforce(
            ProxyContext proxyContext, 
            IExchange exchange, Connection connectionInfo)
        {
            if (Filter.Apply(exchange))
            {
                foreach (var action in Actions)
                {
                    var res  = await action.Alter(proxyContext, exchange, connectionInfo);

                    if (!res) // Throw that behaviour somewhere 
                        return; 
                }
            }
        }
    }

    public enum ActionTiming
    {
        AfterReceivingRequest,
        BeforeSendingResponse,
    }

    public enum ActionType
    {
        SetRequestHeader,
        AddRequestHeader,
        SetResponseHeader,
        AddResponseHeader,
        SubstituteRequestBody,
        SubstituteResponseBody, 
    }

    public interface IAction
    {
        Task<bool> Alter(ProxyContext proxyContext, IExchange exchange, Connection connection);
    }

    public class AddRequestHeaderAction : IAction
    {
        public string HeaderName { get; set; }

        public string HeaderValue { get; set; }

        public Task<bool> Alter(
            ProxyContext proxyContext, 
            IExchange exchange, 
            Connection connection)
        {
            throw new NotImplementedException("Show how to alter exchange here" +
                                              "in order to add request header action"); 
        }
    }


    public class ProxyContext
    {
        public HashSet<string> ByPassHosts { get; set; }

        public Dictionary<string, string> CustomSolving { get; set; }
    }


}