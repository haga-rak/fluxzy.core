using System.Text.Json;
using System.Text.Json.Serialization;
using Fluxzy.Misc.Converters;
using Fluxzy.Rules;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    public class GlobalArchiveOption
    {
        public static JsonSerializerOptions JsonSerializerOptions => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new ReadonlyMemoryCharConverter(),
                new JsonStringEnumConverter(),
                new IpAddressConverter(),
                new PolymorphicConverter<Filter>(),
                new PolymorphicConverter<Action>(),
            }
        };
    }
}