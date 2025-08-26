using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Writers;
using Org.BouncyCastle.Tls;

namespace Samples.No019.HttpMessageHandlerWithCustomFingerPrint
{
    internal class Program
    {
        /// <summary>
        ///  This sample shows how to use FluxzyDefaultHandler with a custom TLS/H2 fingerprint.
        /// </summary>
        /// <param name="args"></param>
        static async Task Main(string[] args)
        {
            using var handler = new FluxzyDefaultHandler(SslProvider.BouncyCastle, ITcpConnectionProvider.Default, new EventOnlyArchiveWriter());

            var configuration = CreateCustomImpersonationConfiguration();

            var tlsFingerPrint = TlsFingerPrint.ParseFromImpersonateConfiguration(configuration);

            handler.ConfigureContext = (exchangeContext) => {
                exchangeContext.AdvancedTlsSettings.TlsFingerPrint = tlsFingerPrint;
                exchangeContext.AdvancedTlsSettings.H2StreamSetting = configuration.H2Settings.ToH2StreamSetting();
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            // make request
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://check.ja3.zone/");

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Create a custom impersonation configuration.
        /// </summary>
        /// <returns></returns>
        internal static ImpersonateConfiguration CreateCustomImpersonationConfiguration()
        {
            var networkSettings = new ImpersonateNetworkSettings(
                "772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,45-0-65037-17513-35-10-13-65281-16-51-23-27-18-43-11-5,4588-29-23-24,0",
                false,
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
                earlySharedGroups: new int[] {
                    NamedGroup.X25519MLKEM768,
                    NamedGroup.x25519,
                }
                );

            // Con
            var h2Settings = new ImpersonateH2Setting(new List<ImpersonateH2SettingItem>() {
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsHeaderTableSize, 65536),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsEnablePush, 0),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsInitialWindowSize, 6291456),
                new ImpersonateH2SettingItem(SettingIdentifier.SettingsMaxHeaderListSize, 262144),
            }, true);

            var configuration = new ImpersonateConfiguration(networkSettings, h2Settings, new ());

            return configuration;

        }
    }
}
