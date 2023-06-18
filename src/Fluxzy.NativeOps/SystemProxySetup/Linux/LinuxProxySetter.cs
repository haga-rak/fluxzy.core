// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Core.Proxy;
using Fluxzy.Misc;

namespace Fluxzy.NativeOps.SystemProxySetup.Linux
{
    internal class LinuxProxySetter : ISystemProxySetter
    {
        private readonly Dictionary<string, object> _defaultValues = new Dictionary<string, object> {
            { "org.gnome.system.proxy mode", "none" },
            { "org.gnome.system.proxy use-same-proxy", true },
            { "org.gnome.system.proxy autoconfig-url", string.Empty },
            { "org.gnome.system.proxy.http authentication-password", string.Empty },
            { "org.gnome.system.proxy.http authentication-user", string.Empty },
            { "org.gnome.system.proxy ignore-hosts", new[] { "127.0.0.1/8", "localhost" } },
            { "org.gnome.system.proxy.http use-authentication", false },
            { "org.gnome.system.proxy.http enabled", true },
            { "org.gnome.system.proxy.http host", string.Empty },
            { "org.gnome.system.proxy.http port", 8080 }
        };

        private readonly EnvProxySetter _internalSetter = new EnvProxySetter();

        public async Task ApplySetting(SystemProxySetting proxySetting)
        {
            if (ProcessUtils.IsCommandAvailable("gsettings")) {
                // Gnome based process we set proxy settings via gsettings

                if (!proxySetting.Enabled) {
                    if (proxySetting.PrivateValues.TryGetValue("GSettings.Proxy", out var prev)
                        && prev is Dictionary<string, object> previousValues) {
                        // Restore the existing settings

                        foreach (var (key, value) in previousValues) {
                            SetGSettingValue(key, value);
                        }

                        return;
                    }

                    // Just disable proxy 

                    await ProcessUtils.QuickRunAsync("gsettings set org.gnome.system.proxy mode 'none'");

                    return;
                }

                SetGSettingValue("org.gnome.system.proxy mode", "manual");
                SetGSettingValue("org.gnome.system.proxy use-same-proxy", true);
                SetGSettingValue("org.gnome.system.proxy.http host", proxySetting.BoundHost);
                SetGSettingValue("org.gnome.system.proxy.http port", proxySetting.ListenPort);
            }
        }

        public Task<SystemProxySetting> ReadSetting()
        {
            if (ProcessUtils.IsCommandAvailable("gsettings")) {
                // Gnome based process we set proxy settings via gsettings

                var previousSettings = new Dictionary<string, object?>();

                foreach (var (key, value) in _defaultValues) {
                    var res = ReadGSettingValue(key);

                    if (res != null)
                        previousSettings.Add(key, JsonSerializer.Deserialize(res.ToRegularJson(), value.GetType()));
                    else
                        previousSettings.Add(key, value);
                }

                var finalSettings = new SystemProxySetting(
                    previousSettings["org.gnome.system.proxy.http host"]!.ToString(),
                    (int) previousSettings["org.gnome.system.proxy.http port"]!,
                    previousSettings["org.gnome.system.proxy ignore-hosts"] == null
                        ? Array.Empty<string>()
                        : (string[]) previousSettings["org.gnome.system.proxy ignore-hosts"]!
                ) {
                    Enabled = previousSettings["org.gnome.system.proxy mode"]?.ToString() == "manual"
                              && (bool) previousSettings["org.gnome.system.proxy.http enabled"]!,
                    PrivateValues = {
                        ["GSettings.Proxy"] = previousSettings
                    }
                };

                return Task.FromResult(finalSettings);
            }

            // Ignore if not working 

            return _internalSetter.ReadSetting();
        }

        private string? ReadGSettingValue(string key)
        {
            var result = ProcessUtils.QuickRun($"gsettings get {key}");

            if (result.ExitCode != 0)
                return null;

            return result?.StandardOutputMessage?.Trim(' ', '\r', '\n');
        }

        private bool SetGSettingValue(string key, string value)
        {
            var result = ProcessUtils.QuickRun($"gsettings set {key}  \"{value}\"");

            return result.ExitCode == 0;
        }

        private bool SetGSettingValue(string key, object value)
        {
            var result = ProcessUtils.QuickRun($"gsettings set {key}  " +
                                               $"\"{JsonSerializer.Serialize(value, value.GetType()).ToGtkJson()}\"");

            return result.ExitCode == 0;
        }
    }

    internal static class JsonPatch
    {
        /// <summary>
        ///     Following methods are utterly bad but enough for the expected gsettings result
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static string ToRegularJson(this string str)
        {
            return str.Replace("\'", "\"");
        }

        internal static string ToGtkJson(this string str)
        {
            return str.Replace("\"", "\'");
        }
    }
}
