// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.IO;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Readers;

namespace Fluxzy.Formatters.Producers.ProducerActions.Actions
{
    public class SaveWebSocketBodyAction
    {
        private readonly IArchiveReader _archiverReader;

        public SaveWebSocketBodyAction(IArchiveReader archiverReader)
        {
            _archiverReader = archiverReader;
        }

        public async Task<bool> Do(int exchangeId, int messageId, WsMessageDirection direction, string filePath)
        {
            if (direction == WsMessageDirection.Sent) {
                using var stream =
                    _archiverReader.GetRequestWebsocketContent(exchangeId, messageId);

                if (stream == null)
                    return false;

                using var outStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                await stream.CopyToAsync(outStream);
                return true; 
            }

            if (direction == WsMessageDirection.Receive) {
                using var stream =
                    _archiverReader.GetRequestWebsocketContent(exchangeId, messageId);

                if (stream == null)
                    return false;

                using var outStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                await stream.CopyToAsync(outStream);
                return true; 
            }

            return false;
        }

    }
}