// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace Fluxzy.Build
{
    /// <summary>
    ///     This program contains the main pipelines for buliding the project.
    /// </summary>
    internal class Program
    {
        private static readonly bool SkipSigning =
            string.Equals(Environment.GetEnvironmentVariable("NO_SIGN"), "1");

        private static readonly int ConcurrentSignCount =
            int.Parse(Environment.GetEnvironmentVariable("CONCURRENT_SIGN")?.Trim() ?? "6");

        private static readonly HttpClient Client = new(new HttpClientHandler());

        private static readonly SemaphoreSlim SemaphoreSlim = new(ConcurrentSignCount);

        public static Dictionary<OSPlatform, string[]> TargetRuntimeIdentifiers { get; } = new() {
            [OSPlatform.Windows] = new[] { "win-x64", "win-x86", "win-arm64" },
            [OSPlatform.Linux] = new[] { "linux-x64", "linux-arm64" },
            [OSPlatform.OSX] = new[] { "osx-x64", "osx-arm64" }
        };

        /// <summary>
        ///     Obtains GetEvOrFail or throw exception if not found.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        private static string GetEvOrFail(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName) ??
                   throw new Exception($"Environment variable \"{variableName}\" must be SET");
        }

        private static async Task<string> GetRunningVersion()
        {
            // nbgv get-version -v Version
            var (stdOut, _) = await ReadAsync("nbgv", "get-version -v Version");

            return stdOut.Trim();
        }

        private static string GetFileName(string runtimeIdentifier, string version)
        {
            return $"fluxzy-cli-{version}-{runtimeIdentifier}.zip";
        }

        private static async Task SignCli(string workingDirectory, IEnumerable<FileInfo> signableFiles)
        {
            var azureVaultDescriptionUrl = GetEvOrFail("AZURE_VAULT_DESCRIPTION_URL");
            var azureVaultUrl = GetEvOrFail("AZURE_VAULT_URL");
            var azureVaultCertificate = GetEvOrFail("AZURE_VAULT_CERTIFICATE");
            var azureVaultClientId = GetEvOrFail("AZURE_VAULT_CLIENT_ID");
            var azureVaultClientSecret = GetEvOrFail("AZURE_VAULT_CLIENT_SECRET");
            var azureVaultTenantId = GetEvOrFail("AZURE_VAULT_TENANT_ID");

            foreach (var file in signableFiles) {
                try {
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
                finally {
                    SemaphoreSlim.Release();
                }
            }
        }

        private static async Task SignPackages(string workingDirectory)
        {
            var azureVaultDescriptionUrl = GetEvOrFail("AZURE_VAULT_DESCRIPTION_URL");
            var azureVaultUrl = GetEvOrFail("AZURE_VAULT_URL");
            var azureVaultCertificate = "FluxzyCodeSigningGS"; // GetEvOrFail("AZURE_VAULT_CERTIFICATE");
            var azureVaultClientId = GetEvOrFail("AZURE_VAULT_CLIENT_ID");
            var azureVaultClientSecret = GetEvOrFail("AZURE_VAULT_CLIENT_SECRET");
            var azureVaultTenantId = GetEvOrFail("AZURE_VAULT_TENANT_ID");

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

        private static string GetSha512Hash(FileInfo file)
        {
            using var sha512 = SHA512.Create();
            using var stream = file.OpenRead();
            var hash = sha512.ComputeHash(stream);

            return Convert.ToHexString(hash).ToLower();
        }

        private static async Task Upload(FileInfo fullFile)
        {
            var uploadReleaseToken = GetEvOrFail("UPLOAD_RELEASE_TOKEN");

            var hashValue = GetSha512Hash(fullFile);
            var fileName = fullFile.Name;
            var finalUrl = "https://upload.fluxzy.io:4300/release/upload";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, finalUrl);

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", uploadReleaseToken);

            var boundary = Guid.NewGuid().ToString();
            var multipartFormContent = new MultipartFormDataContent(boundary);

            var dictionary = new Dictionary<string, string> {
                ["FileName"] = fileName,
                ["FileSha512Hash"] = hashValue,
                ["Category"] = "Fluxzy CLI"
            };

            await using var uploadStream = fullFile.OpenRead();

            // multipartFormContent.Add(new FormUrlEncodedContent(dictionary));

            foreach (var (name, value) in dictionary) {
                multipartFormContent.Add(new StringContent(value), name);
            }

            multipartFormContent.Add(new StreamContent(uploadStream), "FILENAME", fileName);

            requestMessage.Content = multipartFormContent;

            var response = await Client.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode) {
                var fullResponseText = await response.Content.ReadAsStringAsync();

                throw new Exception($"Upload failed {response.StatusCode}.\r\n" +
                                    $"{fullResponseText}");
            }
        }

        private static void AddBasicBuildTargets(string privateNugetToken)
        {
            Target("add-nuget-source",
                async () => {
                    await RunAsync("dotnet",
                        "nuget add source https://nuget.pkg.github.com/haga-rak/index.json " +
                        $"-n nuget-fluxy -u haga-rak -p {privateNugetToken}", handleExitCode: _ => true, noEcho: true);
                });

            Target("restore-tests",
                DependsOn("add-nuget-source"),
                async () => {
                    await RunAsync("dotnet",
                        "restore test/Fluxzy.Tests");
                });

            Target("restore-fluxzy-core",
                DependsOn("add-nuget-source"),
                async () => {
                    await RunAsync("dotnet",
                        "restore src/Fluxzy.Core");
                });

            Target("build-fluxzy-core",
                DependsOn("restore-fluxzy-core"),
                async () => {
                    await RunAsync("dotnet",
                        "build src/Fluxzy.Core  --no-restore");
                });

            Target("tests",
                DependsOn("restore-tests", "build-fluxzy-core"),
                async () => {
                    await RunAsync("dotnet",
                        "test test/Fluxzy.Tests -e EnableDumpStackTraceOn502=true");
                });
        }
        
        private static async Task CreateAndPushVersionedTag(string suffix)
        {
            var runningVersion = (await GetRunningVersion()) + suffix;

            await RunAsync("git", $"config --global user.email \"admin@fluxzy.io\"");
            await RunAsync("git", $"config --global user.name \"fluxzy-ci\"");

            await RunAsync("git", $"tag -a v{runningVersion} -m \"Release {runningVersion}\"", handleExitCode: i => true);
            await RunAsync("git", $"push origin v{runningVersion}", handleExitCode: i => true);
        }

        private static async Task Main(string[] args)
        {
            var (stdOut, _) = await ReadAsync("git", "branch --show-current");
            var currentBranch = stdOut.Trim();

            var privateNugetToken = GetEvOrFail("TOKEN_FOR_NUGET");
            var partnerNugetToken = GetEvOrFail("PARTNER_SECRET");

            // Why there's no better way to do it?
            var current = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : OSPlatform.Windows;

            if (Directory.Exists("_npkgout")) {
                Directory.Delete("_npkgout", true);
            }

            Target("must-be-release",
                () => {
                    if (!currentBranch.StartsWith("release/")) {
                        throw new Exception($"Must be on release branch. Current branch is {currentBranch}");
                    }
                });

            AddBasicBuildTargets(privateNugetToken);

            Target("install-tools",
                async () => {
                    await RunAsync("dotnet",
                        "tool install --global dotnet-script", handleExitCode: _ => true);

                    await RunAsync("dotnet",
                        "tool install --global nbgv", handleExitCode: _ => true);

                    await RunAsync("dotnet",
                        "tool install --global dotnet-project-licenses", handleExitCode: _ => true);
                });

            Target("fluxzy-core-create-package",
                DependsOn("install-tools"),
                async () => {
                    await RunAsync("dotnet",
                        "build -c Release src/Fluxzy.Core");

                    await RunAsync("dotnet",
                        "pack -c Release src/Fluxzy.Core -o _npkgout");
                });

            Target("fluxzy-core-pcap-create-package",
                DependsOn("fluxzy-core-create-package"),
                async () => {
                    await RunAsync("dotnet",
                        "build -c Release src/Fluxzy.Core.Pcap");

                    await RunAsync("dotnet",
                        "pack -c Release src/Fluxzy.Core.Pcap -o _npkgout");
                });

            Target("fluxzy-package-sign",
                DependsOn("fluxzy-core-pcap-create-package"),
                async () => { await SignPackages("_npkgout"); });

            Target("fluxzy-package-push-github",
                DependsOn("fluxzy-package-sign"),
                async () => {
                    await RunAsync("dotnet",
                        $"nuget push *.nupkg --skip-duplicate --api-key {privateNugetToken} " +
                        "--source https://nuget.pkg.github.com/haga-rak/index.json",
                        "_npkgout", true);
                });

            Target("fluxzy-package-push-partner",
                DependsOn("fluxzy-package-sign"),
                async () => {
                    await RunAsync("dotnet",
                        $"nuget push *.nupkg --skip-duplicate --api-key {partnerNugetToken} " +
                        "--source https://nuget.2befficient.io/v3/index.json",
                        "_npkgout", true);
                });

            Target("fluxzy-package-push-public-internal",
                DependsOn("fluxzy-package-sign"),
                async () => {
                    var nugetOrgApiKey = GetEvOrFail("NUGET_ORG_API_KEY");

                    await RunAsync("dotnet",
                        $"nuget push *.nupkg --skip-duplicate --api-key {nugetOrgApiKey} " +
                        "--source https://api.nuget.org/v3/index.json",
                        "_npkgout", true);
                });

            Target("fluxzy-cli-package-build",
                DependsOn("install-tools", "build-fluxzy-core"),
                async () => {
                    if (Directory.Exists(".artefacts")) {
                        Directory.Delete(".artefacts", true);
                    }

                    foreach (var runtimeIdentifier in TargetRuntimeIdentifiers[current]) {
                        var outDirectory = $".artefacts/{runtimeIdentifier}";

                        await RunAsync("dotnet",
                            $"publish --sc true -c Release -r {runtimeIdentifier} \"/p:DebugType=None\" \"/p:DebugSymbols=false\" -o {outDirectory} \"./src/Fluxzy");
                    }
                });

            Target("fluxzy-cli-package-zip",
                DependsOn("fluxzy-cli-package-sign"),
                async () => {
                    var runningVersion = await GetRunningVersion();
                    Directory.CreateDirectory(".artefacts/final");

                    foreach (var runtimeIdentifier in TargetRuntimeIdentifiers[current]) {
                        var outDirectory = $".artefacts/{runtimeIdentifier}";

                        ZipFile.CreateFromDirectory(
                            outDirectory,
                            $".artefacts/final/{GetFileName(runtimeIdentifier, runningVersion)}",
                            CompressionLevel.Optimal,
                            false
                        );
                    }
                });

            Target("fluxzy-cli-publish-internal",
                DependsOn("fluxzy-cli-package-zip"),
                async () => {
                    var candidates = new DirectoryInfo(".artefacts/final/").EnumerateFiles("*.zip").ToList();

                    var uploadTasks = new List<Task>();

                    foreach (var candidate in candidates) {
                        uploadTasks.Add(Upload(candidate));
                    }

                    await Task.WhenAll(uploadTasks);
                });

            Target("fluxzy-cli-package-sign",
                DependsOn("fluxzy-cli-package-build"),
                async () => {
                    if (current != OSPlatform.Windows) {
                        Console.WriteLine("Skipping signing for non-windows platform");

                        return;
                    }

                    if (SkipSigning) {
                        return;
                    }

                    Directory.CreateDirectory(".artefacts/final");

                    var signedFilesPrefix = new[] {
                        "Flux", "Yaml", "ICSharpCode", "BouncyCastle.Crypto.Async", "YamlDotNet", "MessagePack",
                        "UAParser", "SharpPcap"
                    };

                    var signTasks = new List<Task>();

                    foreach (var runtimeIdentifier in TargetRuntimeIdentifiers[current]) {
                        var outDirectory = $".artefacts/{runtimeIdentifier}";

                        var candidates = new DirectoryInfo(outDirectory)
                                         .EnumerateFiles("*", SearchOption.AllDirectories)
                                         .Where(f =>
                                             (
                                                 string.Equals(f.Extension, ".dll", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(f.Extension, ".exe",
                                                     StringComparison.OrdinalIgnoreCase)) &&
                                             signedFilesPrefix.Any(s =>
                                                 f.Name.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
                                         .ToList();

                        // SIGN dll and exe in this directory

                        signTasks.Add(SignCli(outDirectory, candidates));
                    }

                    await Task.WhenAll(signTasks);
                });


            Target("docs", async () => {
                var docOutputPath = GetEvOrFail("DOCS_OUTPUT_PATH");

                await RunAsync("dotnet",
                    $"run --project src/Fluxzy.Tools.DocGen",
                    configureEnvironment: env => env["DOCS_OUTPUT_PATH"] = docOutputPath,
                    noEcho: false);
            });

            Target("default", () => Console.WriteLine("DefaultTarget is doing nothing"));

            // Validate current branch
            Target("validate-main", DependsOn("tests"));

            // Validate a pull request 
            Target("on-pull-request", DependsOn("tests"));

            // Build local CLI packages signed 
            Target("fluxzy-publish-nuget",
                DependsOn("install-tools", "fluxzy-package-push-github", "fluxzy-package-push-partner"),
                async () => await CreateAndPushVersionedTag(""));

            Target("fluxzy-publish-nuget-public",
                DependsOn("install-tools", "must-be-release", "fluxzy-publish-nuget", "fluxzy-package-push-public-internal"),
                async () => await CreateAndPushVersionedTag(""));

            Target("fluxzy-cli-full-package", DependsOn("fluxzy-cli-package-zip"));

            // Build local CLI packages signed 
            Target("fluxzy-cli-publish", DependsOn("install-tools", "fluxzy-cli-publish-internal"),
                async () => await CreateAndPushVersionedTag("-cli"));

            await RunTargetsAndExitAsync(args, ex => ex is ExitCodeException);
        }
    }
}
