// Copyright © 2022 Haga Rakotoharivelo

using System.Text.Json;

namespace Echoes
{
    public class StartupConfigSetting
    {
        public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }; 
    }
}