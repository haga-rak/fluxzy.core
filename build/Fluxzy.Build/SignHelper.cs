// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using static SimpleExec.Command;

namespace Fluxzy.Build
{
    internal static class SignHelper
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new(BuildSettings.ConcurrentSignCount);

        public static async Task SignCli(string workingDirectory, IEnumerable<FileInfo> signableFiles)
        {
            var azureVaultDescriptionUrl = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_DESCRIPTION_URL");
            var azureVaultUrl = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_URL");
            var azureVaultCertificate = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_CERTIFICATE");
            var azureVaultClientId = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_CLIENT_ID");
            var azureVaultClientSecret = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_CLIENT_SECRET");
            var azureVaultTenantId = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_TENANT_ID");

            foreach (var file in signableFiles)
            {
                try
                {
                    await SemaphoreSlim.WaitAsync();

                    await RunAsync("sign",
                        $"code azure-key-vault \"{file.FullName}\" " +
                        "  --publisher-name \"Fluxzy SAS\"" +
                        " --description \"Fluxzy Signed\"" +
                        $" --description-url {azureVaultDescriptionUrl}" +
                        $" --azure-key-vault-url {azureVaultUrl}" +
                        $" --azure-key-vault-certificate {azureVaultCertificate}" +
                        $" --azure-key-vault-client-id {azureVaultClientId}" +
                        $" --azure-key-vault-client-secret {azureVaultClientSecret}" +
                        $" --azure-key-vault-tenant-id {azureVaultTenantId}"
                        , noEcho: true,
                        workingDirectory: workingDirectory
                    );
                }
                finally
                {
                    SemaphoreSlim.Release();
                }
            }
        }

        public static async Task SignPackages(string workingDirectory)
        {
            var azureVaultDescriptionUrl = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_DESCRIPTION_URL");
            var azureVaultUrl = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_URL");
            var azureVaultCertificate = "FluxzyCodeSigningGS"; // EnvironmentHelper.GetEvOrFail("AZURE_VAULT_CERTIFICATE");
            var azureVaultClientId = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_CLIENT_ID");
            var azureVaultClientSecret = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_CLIENT_SECRET");
            var azureVaultTenantId = EnvironmentHelper.GetEvOrFail("AZURE_VAULT_TENANT_ID");

            await RunAsync("sign",
                "code azure-key-vault *.nupkg " +
                "  --publisher-name \"Fluxzy SAS\"" +
                " --description \"Fluxzy Signed\"" +
                $" --description-url {azureVaultDescriptionUrl}" +
                $" --azure-key-vault-url {azureVaultUrl}" +
                $" --azure-key-vault-certificate {azureVaultCertificate}" +
                $" --azure-key-vault-client-id {azureVaultClientId}" +
                $" --azure-key-vault-client-secret {azureVaultClientSecret}" +
                $" --azure-key-vault-tenant-id {azureVaultTenantId}"
                , noEcho: true,
                workingDirectory: workingDirectory
            );
        }

    }
}
