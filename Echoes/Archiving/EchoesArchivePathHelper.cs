using Echoes.Clients;
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
        internal static string GetRequestEntry(Exchange message)
        {
            return $"content/{message.Id}/request/";
        }
        
        internal static string GetResponseEntry(Exchange message)
        {
            return $"content/{message.Id}/request/response/{message.Id}";
        }
        
        /// <summary>
        /// Obtient le répertoire relative au système de zip 
        /// </summary>
        /// <param name="exchange"></param>
        /// <returns></returns>
        internal static string GetMessageEntryName(Exchange exchange)
        {
            return $"data/{exchange.Id}/def.json";
        }
    }
}