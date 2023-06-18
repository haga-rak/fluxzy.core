// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Core.Proxy;

namespace Fluxzy.NativeOps.SystemProxySetup.Linux
{
    /// <summary>
    ///     Update system proxy setting by editing HTTPS_PROXY global variable
    ///     common for macOS and linux
    /// </summary>
    internal class EnvProxySetter : ISystemProxySetter
    {
        public Task ApplySetting(SystemProxySetting value)
        {
            if (value.ListenPort < 0 || string.IsNullOrEmpty(value.BoundHost)) {
                // Clear env variable 

                // TODO : Find other ways to set variable, this is working only for Windows 

                Environment.SetEnvironmentVariable("http_proxy", "", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("https_proxy", "", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("no_proxy", "", EnvironmentVariableTarget.User);
            }

            var word = $"http://{value.BoundHost}:{value.ListenPort}";
            var noProxy = string.Join(",", value.ByPassHosts);

            Environment.SetEnvironmentVariable("https_proxy", word, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("http_proxy", word, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("no_proxy", word, EnvironmentVariableTarget.User);

            return Task.CompletedTask;
        }

        public Task<SystemProxySetting> ReadSetting()
        {
            var httpProxySetting = Environment.GetEnvironmentVariable("http_proxy", EnvironmentVariableTarget.User)
                                              ?.Trim();

            var httpsProxySetting = Environment.GetEnvironmentVariable("https_proxy", EnvironmentVariableTarget.User)
                                               ?.Trim();

            var noProxy = Environment.GetEnvironmentVariable("no_proxy", EnvironmentVariableTarget.User)?.Trim();
            var byPassHosts = Array.Empty<string>();

            if (!string.IsNullOrEmpty(noProxy)) {
                byPassHosts = noProxy.Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(x => x.Trim())
                                     .ToArray();
            }

            if (!string.IsNullOrEmpty(httpsProxySetting)) {
                var splitTab = httpsProxySetting.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                if (splitTab.Length > 1 && int.TryParse(splitTab.Last(), out var port) && port > 0 && port < 65535) {
                    return Task.FromResult(new SystemProxySetting(string.Join(":", splitTab.SkipLast(1)), port, byPassHosts) {
                        Enabled = true
                    });
                }
            }

            if (!string.IsNullOrEmpty(httpProxySetting)) {
                var splitTab = httpProxySetting.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                if (splitTab.Length > 1 && int.TryParse(splitTab.Last(), out var port) && port > 0 && port < 65535) {
                    return Task.FromResult(new SystemProxySetting(string.Join(":", splitTab.SkipLast(1)), port, byPassHosts) {
                        Enabled = true
                    });
                }
            }

            return Task.FromResult(new SystemProxySetting("", -1) {
                Enabled = false
            });
        }
    }
}
