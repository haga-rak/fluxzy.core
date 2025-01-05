// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Rules.Actions
{
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
        
        public ImpersonateNetworkSettings NetworkSettings { get; }

        public ImpersonateH2Setting H2Settings { get; }

        public List<ImpersonateHeader> Headers { get; }

    }

    public class ImpersonateNetworkSettings
    {
        public ImpersonateNetworkSettings(string ja3FingerPrint, bool? greaseMode, Dictionary<int, byte[]>? overrideClientExtensionsValues)
        {
            Ja3FingerPrint = ja3FingerPrint;
            GreaseMode = greaseMode;
            OverrideClientExtensionsValues = overrideClientExtensionsValues;
        }

        public string Ja3FingerPrint { get; }

        public bool? GreaseMode { get; }
        
        public Dictionary<int, byte[]>? OverrideClientExtensionsValues { get; }
    }

    public class ImpersonateH2SettingItem
    {
        public ImpersonateH2SettingItem(SettingIdentifier identifier, int value)
        {
            Identifier = identifier;
            Value = value;
        }

        public SettingIdentifier Identifier { get;  }

        public int Value { get; }
    }

    public class ImpersonateH2Setting
    {
        public ImpersonateH2Setting(List<ImpersonateH2SettingItem> settings, bool removeDefaultValues)
        {
            Settings = settings;
            RemoveDefaultValues = removeDefaultValues;
        }

        public List<ImpersonateH2SettingItem> Settings { get; }

        public bool RemoveDefaultValues { get;  }
    }
    
    public class ImpersonateHeader
    {
        public ImpersonateHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
