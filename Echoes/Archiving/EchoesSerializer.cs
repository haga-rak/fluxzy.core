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

        public static Exchange FromSerializedString(string content)
        {
            return JsonConvert.DeserializeObject<Exchange>(content, DefaultSetting); 
        }

        public static string ToSerializedString(this Exchange httpMessage)
        {
            return JsonConvert.SerializeObject(httpMessage, Formatting.None, DefaultSetting);
        }
    }
}