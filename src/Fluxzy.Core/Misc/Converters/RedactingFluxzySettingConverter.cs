// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Fluxzy.Misc.Converters
{
    /// <summary>
    ///     Serializes <see cref="FluxzySetting"/> for embedding in archive meta information,
    ///     scrubbing credentials and local file paths. Reading is delegated to the default
    ///     contract so consumers see a regular <see cref="FluxzySetting"/>.
    /// </summary>
    internal class RedactingFluxzySettingConverter : JsonConverter<FluxzySetting>
    {
        public override FluxzySetting? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<FluxzySetting>(ref reader, GlobalArchiveOption.ConfigSerializerOptions);
        }

        public override void Write(Utf8JsonWriter writer, FluxzySetting value, JsonSerializerOptions options)
        {
            var node = JsonSerializer.SerializeToNode(value, GlobalArchiveOption.ConfigSerializerOptions)?.AsObject();

            if (node == null) {
                writer.WriteNullValue();
                return;
            }

            Redact(node);

            node.WriteTo(writer);
        }

        private static void Redact(JsonObject root)
        {
            if (root["caCertificate"] is JsonObject cert) {
                cert.Remove("pkcs12File");
                cert.Remove("pkcs12Password");
            }

            if (root["proxyAuthentication"] is JsonObject auth) {
                auth.Remove("password");
            }

            root.Remove("certificateCacheDirectory");
            root.Remove("userAgentActionConfigurationFile");

            if (FluxzySharedSetting.RedactSettingsInArchive) {
                root.Remove("internalAlterationRules");
                root.Remove("saveFilter");
            }
        }
    }
}
