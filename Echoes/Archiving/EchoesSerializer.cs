using Newtonsoft.Json;

namespace Echoes
{
    internal static class HttpMessageSerializationExtensions
    {
        private static readonly JsonSerializerSettings DefaultSetting = new JsonSerializerSettings()
        {
            // Permet de gérer les héritages dans la déserialization
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static HttpExchange FromSerializedString(string content)
        {
            return JsonConvert.DeserializeObject<HttpExchange>(content, DefaultSetting); 
        }

        public static string ToSerializedString(this HttpExchange httpMessage)
        {
            return JsonConvert.SerializeObject(httpMessage, Formatting.None, DefaultSetting);
        }
    }
}