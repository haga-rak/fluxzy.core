// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Channels;
using Fluxzy.Misc;

namespace Fluxzy.Desktop.Services
{
    public class ForwardMessageManager
    {
        private readonly Channel<ForwardMessage> _bufferedChannel = Channel.CreateUnbounded<ForwardMessage>();

        public void Send<T>(T payload) where T : notnull
        {
            _bufferedChannel.Writer.TryWrite(new ForwardMessage(payload.GetType().Name, payload)); 
        }

        public async Task<List<ForwardMessage>> ReadAll()
        {
            var list = new List<ForwardMessage>();

            await _bufferedChannel.Reader.WaitToReadAsync();

            _bufferedChannel.Reader.TryReadAll(list);

            return list;
        }
    }

    public class ForwardMessage
    {
        public ForwardMessage(string type, object payload)
        {
            Type = type;
            Payload = payload;
        }

        public string Type { get;  }

        public object Payload { get;  }
    }
}