// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fluxzy.Readers
{
    internal static class ArchiveMetaInformationReader
    {
        /// <summary>
        ///     Deserializes a meta.json blob, falling back to a meta with a null
        ///     <see cref="ArchiveMetaInformation.CapturedSetting"/> when the embedded
        ///     setting can't be deserialized (e.g. a Filter/Action discriminator from a
        ///     newer plugin).
        /// </summary>
        public static ArchiveMetaInformation Read(byte[] bytes)
        {
            try {
                return JsonSerializer.Deserialize<ArchiveMetaInformation>(bytes,
                    GlobalArchiveOption.DefaultSerializerOptions) ?? new ArchiveMetaInformation();
            }
            catch (JsonException) {
                var node = JsonNode.Parse(bytes);

                if (node is JsonObject obj && obj.ContainsKey("capturedSetting")) {
                    obj.Remove("capturedSetting");

                    return JsonSerializer.Deserialize<ArchiveMetaInformation>(obj.ToJsonString(),
                        GlobalArchiveOption.DefaultSerializerOptions) ?? new ArchiveMetaInformation();
                }

                throw;
            }
        }
    }
}
