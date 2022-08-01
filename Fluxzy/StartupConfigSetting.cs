﻿// Copyright © 2022 Haga Rakotoharivelo

using System.Text.Json;

namespace Fluxzy
{
    public class StartupConfigSetting
    {
        public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }; 
    }
}