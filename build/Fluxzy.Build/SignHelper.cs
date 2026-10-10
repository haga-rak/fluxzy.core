// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using static SimpleExec.Command;

namespace Fluxzy.Build
{
    /// <summary>
    ///     Authenticode signing with Azure Artifact Signing through the `sign` global tool
    ///     (dotnet/sign, must have the `artifact-signing` command).
    ///     There is no certificate file: Microsoft holds the key and issues a 3-day certificate,
    ///     so every signature is RFC 3161 timestamped. Authentication goes through
    ///     DefaultAzureCredential: in CI the app registration client secret is passed as
    ///     AZURE_CLIENT_ID / AZURE_TENANT_ID / AZURE_CLIENT_SECRET (EnvironmentCredential, nothing
    ///     to install on the runner), locally `az login` works too. The identity needs the
    ///     "Artifact Signing Certificate Profile Signer" role on the certificate profile.
    ///     Same account and profile as the Fluxzy Desktop repository (new-cert.md).
    /// </summary>
    internal static class SignHelper
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new(BuildSettings.ConcurrentSignCount);

        public static string SignToolVersion => "0.9.1-beta.26475.3";

        private static string Endpoint =>
            Environment.GetEnvironmentVariable("ARTIFACT_SIGNING_ENDPOINT") ?? "https://weu.codesigning.azure.net/";

        private static string Account =>
            Environment.GetEnvironmentVariable("ARTIFACT_SIGNING_ACCOUNT") ?? "fluxzyartifact";

        private static string Profile =>
            Environment.GetEnvironmentVariable("ARTIFACT_SIGNING_PROFILE") ?? "fluxzy-public";

        /// <summary>
        ///     Optional: azure-cli | azure-powershell | managed-identity | workload-identity.
        ///     Unset means DefaultAzureCredential, which tries the AZURE_* environment variables first.
        /// </summary>
        private static string? CredentialType => Environment.GetEnvironmentVariable("AZURE_CREDENTIAL_TYPE");

        public static Task SignCli(string workingDirectory, IEnumerable<FileInfo> signableFiles)
        {
            return Sign(workingDirectory, signableFiles.Select(f => f.Name), "Fluxzy CLI");
        }

        public static Task SignPackages(string workingDirectory)
        {
            var packages = new DirectoryInfo(workingDirectory).EnumerateFiles("*.nupkg").Select(f => f.Name);

            return Sign(workingDirectory, packages, "Fluxzy.Core");
        }

        /// <summary>
        ///     One `sign` invocation per directory: a single Azure authentication for the batch.
        /// </summary>
        private static async Task Sign(string workingDirectory, IEnumerable<string> fileNames, string description)
        {
            var files = fileNames.ToList();

            if (files.Count == 0) {
                Console.WriteLine($"Nothing to sign in {workingDirectory}");

                return;
            }

            var args = string.Join(" ", files.Select(f => $"\"{f}\"")) +
                       $" --base-directory \"{Path.GetFullPath(workingDirectory)}\"" +
                       $" --artifact-signing-endpoint {Endpoint}" +
                       $" --artifact-signing-account {Account}" +
                       $" --artifact-signing-certificate-profile {Profile}" +
                       // Mandatory: the certificate is rotated every 3 days, only the timestamp
                       // keeps the signature valid afterwards
                       " --timestamp-url http://timestamp.acs.microsoft.com" +
                       " --timestamp-digest sha256" +
                       " --file-digest sha256" +
                       " --publisher-name \"Fluxzy SAS\"" +
                       $" --description \"{description}\"" +
                       " --description-url https://www.fluxzy.io" +
                       " --verbosity Information";

            if (!string.IsNullOrWhiteSpace(CredentialType)) {
                args += $" --azure-credential-type {CredentialType}";
            }

            try {
                await SemaphoreSlim.WaitAsync();

                Console.WriteLine($"Signing {files.Count} file(s) in {workingDirectory} with {Account}/{Profile}");

                await RunAsync("sign", "code artifact-signing " + args, noEcho: true);
            }
            finally {
                SemaphoreSlim.Release();
            }
        }
    }
}
