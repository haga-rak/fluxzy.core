// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.Ssl
{
    /// <summary>
    /// Configuration holder for an impersonation profile.
    /// </summary>
    public class ImpersonateConfiguration
    {
        public ImpersonateConfiguration(
            ImpersonateNetworkSettings networkSettings,
            ImpersonateH2Setting h2Settings, List<ImpersonateHeader> headers)
        {
            Headers = headers;
            NetworkSettings = networkSettings;
            H2Settings = h2Settings;
        }

        /// <summary>
        /// Network settings.
        /// </summary>
        public ImpersonateNetworkSettings NetworkSettings { get; }

        /// <summary>
        /// HTTP/2 settings.
        /// </summary>
        public ImpersonateH2Setting H2Settings { get; }

        /// <summary>
        /// Header settings.
        /// </summary>
        public List<ImpersonateHeader> Headers { get; }
    }

    public class ImpersonateNetworkSettings
    {
        public ImpersonateNetworkSettings(string ja3FingerPrint, bool? greaseMode, 
            Dictionary<int, byte[]>? overrideClientExtensionsValues, int[]? signatureAlgorithms, int[]? earlySharedGroups)
        {
            Ja3FingerPrint = ja3FingerPrint;
            GreaseMode = greaseMode;
            OverrideClientExtensionsValues = overrideClientExtensionsValues;
            SignatureAlgorithms = signatureAlgorithms;
            EarlySharedGroups = earlySharedGroups;
        }

        /// <summary>
        /// JA3 fingerprint.
        /// </summary>
        public string Ja3FingerPrint { get; }

        /// <summary>
        /// When null, Grease mode will be inferred from the client extensions. 
        /// </summary>
        public bool? GreaseMode { get; }

        /// <summary>
        /// Override client extensions values.
        /// </summary>
        public Dictionary<int, byte[]>? OverrideClientExtensionsValues { get; }

        /// <summary>
        /// Signature algorithms. Order matters for JA4.
        /// </summary>
        public int[]? SignatureAlgorithms { get; }

        /// <summary>
        /// When using TLS v1.3, the named group on this list will be used for early shared key (key_share extensions).
        /// </summary>
        public int[]? EarlySharedGroups { get; }
    }

    /// <summary>
    ///  http2 announce settings.
    /// </summary>
    public class ImpersonateH2SettingItem
    {
        public ImpersonateH2SettingItem(SettingIdentifier identifier, int value)
        {
            Identifier = identifier;
            Value = value;
        }

        /// <summary>
        /// Setting identifier.
        /// </summary>
        public SettingIdentifier Identifier { get; }

        /// <summary>
        /// Setting value.
        /// </summary>
        public int Value { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ImpersonateH2Setting
    {
        public ImpersonateH2Setting(List<ImpersonateH2SettingItem> settings, bool removeDefaultValues)
        {
            Settings = settings;
            RemoveDefaultValues = removeDefaultValues;
        }

        public List<ImpersonateH2SettingItem> Settings { get; }

        /// <summary>
        /// Remove default values.
        /// </summary>
        public bool RemoveDefaultValues { get; }

        public int? InitialWindowSize { get; set; } = null;
    }

    public class ImpersonateHeader
    {
        public ImpersonateHeader(string name, string value, bool skipIfExists = false)
        {
            Name = name;
            Value = value;
            SkipIfExists = skipIfExists;
        }

        public string Name { get; }

        public string Value { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool SkipIfExists { get; }
    }
}
