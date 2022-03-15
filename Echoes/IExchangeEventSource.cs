// Copyright © 2022 Haga Rakotoharivelo

using System;
using Echoes.Clients;

namespace Echoes
{
    internal interface IExchangeEventSource
    {
        void OnBeforeRequest(BeforeRequestEventArgs e);

        void OnBeforeResponse(BeforeResponseEventArgs e);

        void OnExchangeComplete(ExchangeCompleteEventArgs e);

        void OnConnectionAdded(ConnectionAddedEventArgs e);

        void OnConnectionUpdate(ConnectionUpdateEventArgs e);
    }

    public class NoOpExchangeEventSource : IExchangeEventSource
    {
        public void OnBeforeRequest(BeforeRequestEventArgs e)
        {
        }

        public void OnBeforeResponse(BeforeResponseEventArgs e)
        {
        }

        public void OnExchangeComplete(ExchangeCompleteEventArgs e)
        {
        }

        public void OnConnectionAdded(ConnectionAddedEventArgs e)
        {
        }

        public void OnConnectionUpdate(ConnectionUpdateEventArgs e)
        {
        }
    }


    public class ProxyEventArgs : EventArgs
    {
        public ProxyEventArgs(ProxyExecutionContext executionContext)
        {
            ExecutionContext = executionContext;
        }

        public ProxyExecutionContext ExecutionContext { get; }
    }

    public class BeforeRequestEventArgs : ProxyEventArgs
    {
        public Exchange Exchange { get; }

        public BeforeRequestEventArgs(ProxyExecutionContext context, Exchange exchange)
            : base(context)
        {
            Exchange = exchange;
        }
    }

    public class BeforeResponseEventArgs : ProxyEventArgs
    {
        public Exchange Exchange { get; }

        public BeforeResponseEventArgs(ProxyExecutionContext context, Exchange exchange)
            : base(context)
        {
            Exchange = exchange;
        }
    }


    public class ExchangeCompleteEventArgs : ProxyEventArgs
    {
        public Exchange Exchange { get; }

        public ExchangeCompleteEventArgs(ProxyExecutionContext context, Exchange exchange)
            : base(context)
        {
            Exchange = exchange;
        }
    }

    public class ConnectionAddedEventArgs : ProxyEventArgs
    {
        public Connection Connection { get; }

        public ConnectionAddedEventArgs(ProxyExecutionContext context, Connection connection)
            : base(context)
        {
            Connection = connection;
        }
    }

    public class ConnectionUpdateEventArgs : ProxyEventArgs
    {
        public Connection Connection { get; }

        public ConnectionUpdateEventArgs(ProxyExecutionContext context, Connection connection)
            : base(context)
        {
            Connection = connection;
        }
    }
}