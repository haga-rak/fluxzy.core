using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    public class GlobalArchiveOption
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; } =  new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new ReadonlyMemoryCharConverter(), 
                new JsonStringEnumConverter()
            }
        };
    }
}