// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Clients.Ssl;
using Org.BouncyCastle.Tls;

namespace Fluxzy.Rules.Actions
{
    public static class ImpersonateProfileManager
    {
        public static readonly string Chrome131Windows = "Chrome_Windows_131";
        public static readonly string Chrome131Android = "Chrome_Android_131";
        public static readonly string Edge131Windows = "Edge_Windows_131";
        public static readonly string Firefox133Windows = "Firefox_Windows_133";

        public static IEnumerable<(ImpersonateProfile Agent, ImpersonateConfiguration Configuration)> GetBuiltInProfiles()
        {
            yield return (ImpersonateProfile.Parse(Chrome131Windows), Create_Chrome131_Windows());
            yield return (ImpersonateProfile.Parse(Chrome131Android), Create_Chrome131_Android());

            yield return (ImpersonateProfile.Parse(Edge131Windows), Create_Edge131_Windows());

            yield return (ImpersonateProfile.Parse(Firefox133Windows), Create_Firefox_133_Windows());
        }

        internal static ImpersonateConfiguration Create_Edge131_Windows()
        {
            var networkSettings = new ImpersonateNetworkSettings(
                "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,5-10-35-51-23-43-18-0-27-17513-11-16-65281-13-45-65037,4588-29-23-24,0",
                true,
                null, 
                new int[] {
                    SignatureScheme.ecdsa_secp256r1_sha256,
                    SignatureScheme.rsa_pss_rsae_sha256,
                    SignatureScheme.rsa_pkcs1_sha256,
                    SignatureScheme.ecdsa_secp384r1_sha384,
                    SignatureScheme.rsa_pss_rsae_sha384,
                    SignatureScheme.rsa_pkcs1_sha384,
                    SignatureScheme.rsa_pss_rsae_sha512,
                    SignatureScheme.rsa_pkcs1_sha512,
                }, 
                null
                );

            var h2Settings = new ImpersonateH2Setting(new List<ImpersonateH2SettingItem>() {
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsHeaderTableSize, 65536),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsEnablePush, 0),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsInitialWindowSize, 6291456),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsMaxHeaderListSize, 262144),
            }, true);

            var headers = new List<ImpersonateHeader>
            {
                new ImpersonateHeader("sec-ch-ua", "\"Microsoft Edge\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\""),
                new ImpersonateHeader("sec-ch-ua-mobile", "?0"),
                new ImpersonateHeader("sec-ch-ua-platform", "\"Windows\""),
                new ImpersonateHeader("Upgrade-Insecure-Requests", "1"),
                new ImpersonateHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0"),
                new ImpersonateHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7", true),
                new ImpersonateHeader("Sec-Fetch-Site", "none"),
                new ImpersonateHeader("Sec-Fetch-Mode", "navigate"),
                new ImpersonateHeader("Sec-Fetch-User", "?1"),
                new ImpersonateHeader("Sec-Fetch-Dest", "document"),
                new ImpersonateHeader("Accept-Encoding", "gzip, deflate, br, zstd", true),
                new ImpersonateHeader("Accept-language", "en-US,en;q=0.9", true),
                new ImpersonateHeader("Priority", "u=0, i"),
            };

            var configuration = new ImpersonateConfiguration(networkSettings,
                h2Settings, headers);

            return configuration;

        }

        internal static ImpersonateConfiguration Create_Chrome131_Windows()
        {
            var networkSettings = new ImpersonateNetworkSettings(
                "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,45-0-65037-17513-35-10-13-65281-16-51-23-27-18-43-11-5,4588-29-23-24,0",
                true,
                null, 
                new int[] {
                    SignatureScheme.ecdsa_secp256r1_sha256,
                    SignatureScheme.rsa_pss_rsae_sha256,
                    SignatureScheme.rsa_pkcs1_sha256,
                    SignatureScheme.ecdsa_secp384r1_sha384,
                    SignatureScheme.rsa_pss_rsae_sha384,
                    SignatureScheme.rsa_pkcs1_sha384,
                    SignatureScheme.rsa_pss_rsae_sha512,
                    SignatureScheme.rsa_pkcs1_sha512,
                },
                null
                );

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
                new ImpersonateHeader("Accept-language", "en-US,en;q=0.9", true),
                new ImpersonateHeader("Priority", "u=0, i"),
            };

            var configuration = new ImpersonateConfiguration(networkSettings,
                h2Settings, headers);

            return configuration;

        }

        internal static ImpersonateConfiguration Create_Chrome131_Android()
        {
            var networkSettings = new ImpersonateNetworkSettings(
                "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,27-13-65281-18-43-0-35-10-5-51-11-16-17513-65037-23-45,4588-29-23-24,0",
                true,
                null, 
                new int[] {
                    SignatureScheme.ecdsa_secp256r1_sha256,
                    SignatureScheme.rsa_pss_rsae_sha256,
                    SignatureScheme.rsa_pkcs1_sha256,
                    SignatureScheme.ecdsa_secp384r1_sha384,
                    SignatureScheme.rsa_pss_rsae_sha384,
                    SignatureScheme.rsa_pkcs1_sha384,
                    SignatureScheme.rsa_pss_rsae_sha512,
                    SignatureScheme.rsa_pkcs1_sha512,
                },
                null
                );

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
                new ImpersonateHeader("sec-ch-ua-platform", "\"Android\""),
                new ImpersonateHeader("Upgrade-Insecure-Requests", "1"),
                new ImpersonateHeader("User-Agent", "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36"),
                new ImpersonateHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7", true),
                new ImpersonateHeader("Sec-Fetch-Site", "none"),
                new ImpersonateHeader("Sec-Fetch-Mode", "navigate"),
                new ImpersonateHeader("Sec-Fetch-User", "?1"),
                new ImpersonateHeader("Sec-Fetch-Dest", "document"),
                new ImpersonateHeader("Accept-Encoding", "gzip, deflate, br, zstd", true),
                new ImpersonateHeader("Accept-language", "en-US,en;q=0.9", true),
                new ImpersonateHeader("Priority", "u=0, i"),
            };

            var configuration = new ImpersonateConfiguration(networkSettings,
                h2Settings, headers);

            return configuration;

        }

        internal static ImpersonateConfiguration Create_Firefox_133_Windows()
        {
            var networkSettings = new ImpersonateNetworkSettings(
                "772,4865-4867-4866-49195-49199-52393-52392-49196-49200-49162-49161-49171-49172-156-157-47-53,0-23-65281-10-11-35-16-5-34-51-43-13-45-28-27-65037,4588-29-23-24-25-256-257,0",
                false,
                null,
                new int[] {
                    SignatureScheme.ecdsa_secp256r1_sha256,
                    SignatureScheme.ecdsa_secp384r1_sha384,
                    SignatureScheme.ecdsa_secp521r1_sha512,
                    SignatureScheme.rsa_pss_rsae_sha256,
                    SignatureScheme.rsa_pss_rsae_sha384,
                    SignatureScheme.rsa_pss_rsae_sha512,
                    SignatureScheme.rsa_pkcs1_sha256,
                    SignatureScheme.rsa_pkcs1_sha384,
                    SignatureScheme.rsa_pkcs1_sha512,
                    SignatureScheme.ecdsa_sha1,
                    SignatureScheme.rsa_pkcs1_sha1,
                },
                null);

            var h2Settings = new ImpersonateH2Setting(new List<ImpersonateH2SettingItem>() {
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsHeaderTableSize, 65536),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsEnablePush, 0),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsInitialWindowSize, 6291456),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsMaxHeaderListSize, 262144),
            }, true);

            var headers = new List<ImpersonateHeader>
            {
                new ImpersonateHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0"),
                new ImpersonateHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8", true),
                new ImpersonateHeader("Accept-Encoding", "gzip, deflate, br, zstd", true),
                new ImpersonateHeader("Upgrade-Insecure-Requests", "1"),
                new ImpersonateHeader("Sec-Fetch-Dest", "document"),
                new ImpersonateHeader("Sec-Fetch-Mode", "navigate"),
                new ImpersonateHeader("Sec-Fetch-Site", "none"),
                new ImpersonateHeader("Sec-Fetch-User", "?1"),
                new ImpersonateHeader("Priority", "u=0, i"),
                new ImpersonateHeader("Accept-language", "en-US,en;q=0.9", true),
            };

            var configuration = new ImpersonateConfiguration(networkSettings,
                h2Settings, headers);

            return configuration;

        }
    }
}
