// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Rules.Actions
{
    internal static class PredefinedImpersonateConfigurationLoader
    {
        public static IEnumerable<(ImpersonateAgent Agent, ImpersonateConfiguration Configuration)> GetPredefined()
        {
            yield return (ImpersonateAgent.Parse("Chrome_Windows_131"), Create_Chrome131_Windows());
        }

        public static ImpersonateConfiguration Create_Chrome131_Windows()
        {
            var networkSettings = new ImpersonateNetworkSettings(
                "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,45-0-65037-17513-35-10-13-65281-16-51-23-27-18-43-11-5,4588-29-23-24,0",
                true,
                null);

            var h2Settings = new ImpersonateH2Setting(new List<ImpersonateH2SettingItem>() {
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsHeaderTableSize, 65536),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsEnablePush, 0),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsInitialWindowSize, 6291456),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsMaxHeaderListSize, 262144),
            }, true);

            var headers = new List<ImpersonateHeader>
            {
                new ImpersonateHeader("sec-ch-ua", "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\""),
                new ImpersonateHeader("sec-ch-ua-mobile", "?0"),
                new ImpersonateHeader("sec-ch-ua-platform", "\"Windows\""),
                new ImpersonateHeader("Upgrade-Insecure-Requests", "1"),
                new ImpersonateHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"),
                new ImpersonateHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7", true),
                new ImpersonateHeader("Sec-Fetch-Site", "none"),
                new ImpersonateHeader("Sec-Fetch-Mode", "navigate"),
                new ImpersonateHeader("Sec-Fetch-User", "?1"),
                new ImpersonateHeader("Sec-Fetch-Dest", "document"),
                new ImpersonateHeader("Accept-Encoding", "gzip, deflate, br, zstd", true),
                new ImpersonateHeader("Priority", "u=0, i"),
            };

            var configuration = new ImpersonateConfiguration(networkSettings,
                h2Settings, headers);

            return configuration;

        }
    }
}
