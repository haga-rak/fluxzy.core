// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Channels;
using Fluxzy.Misc;

namespace Fluxzy.Desktop.Services
{
    public class ForwardMessageManager
    {
        private readonly Channel<ForwardMessage> _bufferedChannel = Channel.CreateUnbounded<ForwardMessage>();
        private CancellationTokenSource? _cts;

        public void Send<T>(T payload) where T : notnull
        {
            _bufferedChannel.Writer.TryWrite(new ForwardMessage(payload.GetType().Name, payload));
        }

        public async Task<List<ForwardMessage>> ReadAll()
        {
            var list = new List<ForwardMessage>();

            CancellationToken token;

            lock (this) {
                if (_cts != null) {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }

                _cts = new CancellationTokenSource();
                token = _cts.Token;
            }


            try {
                while (true) {
                    if (!await _bufferedChannel.Reader.WaitToReadAsync(token))
                        break;

                    await Task.Delay(30, token);

                    _bufferedChannel.Reader.TryReadAll(list);

                    if (list.Count > 0)
                        break;
                }
            }
            catch (OperationCanceledException) {
            }

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

        public string Type { get; }

        public object Payload { get; }
    }
}
