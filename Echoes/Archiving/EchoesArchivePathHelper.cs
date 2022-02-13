using Echoes.Core;

namespace Echoes
{
    internal static class EchoesArchivePathHelper
    {
        /// <summary>
        /// Obtient le répertoire relative au système de zip 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static string GetMessageContentEntryName(HttpMessage message)
        {
            if (! (message is Hpm responseMessage))
                return $"content/{message.Id}/request/";

            return $"content/{responseMessage.RequestId}/response/{responseMessage.Id}";
        }
        
        /// <summary>
        /// Obtient le répertoire relative au système de zip 
        /// </summary>
        /// <param name="exchange"></param>
        /// <returns></returns>
        internal static string GetMessageEntryName(HttpExchange exchange)
        {
            return $"data/{exchange.Index}/def.json";
        }
    }
}