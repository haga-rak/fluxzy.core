// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace Fluxzy.Build
{
    /// <summary>
    ///     This program contains the main pipelines for building the project.
    /// </summary>
    internal class Program
    {
        private static readonly HttpClient Client = new(new HttpClientHandler());

        public static Dictionary<OSPlatform, string[]> TargetRuntimeIdentifiers { get; } = new() {
            [OSPlatform.Windows] = new[] { "win-x64", "win-x86", "win-arm64" },
            [OSPlatform.Linux] = new[] { "linux-x64", "linux-arm64" },
            [OSPlatform.OSX] = new[] { "osx-x64", "osx-arm64" }
        };

        private static async Task<string> GetRunningVersion()
        {
            // nbgv get-version -v Version
            var (stdOut, _) = await ReadAsync("nbgv", "get-version -v Version");

            return stdOut.Trim();
        }

        private static async Task<string> GetRunningVersionShort()
        {
            var runningVersion = await GetRunningVersion();
            var version = new Version(runningVersion);

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        private static string GetFileName(string runtimeIdentifier, string version)
        {
            return $"fluxzy-cli-{version}-{runtimeIdentifier}";
        }

        private static async Task Upload(FileInfo fullFile)
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("ENABLE_UPLOAD"), 
                    "1", StringComparison.OrdinalIgnoreCase)) {
                Console.WriteLine("Skipping upload");
                return;
            }
            
            var uploadReleaseToken = EnvironmentHelper.GetEvOrFail("UPLOAD_RELEASE_TOKEN");

            var hashValue = HashHelper.GetSha512Hash(fullFile);
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

        private static void AddBasicBuildTargets()
        {
            Target(Targets.RestoreTests,
                async () => {
                    await RunAsync("dotnet",
                        "restore test/Fluxzy.Tests");
                });

            Target(Targets.RestoreFluxzyCore,
                async () => {
                    await RunAsync("dotnet",
                        "restore src/Fluxzy.Core");
                });

            Target(Targets.BuildFluxzyCore,
                DependsOn(Targets.RestoreFluxzyCore),
                async () => {
                    await RunAsync("dotnet",
                        "build src/Fluxzy.Core  --no-restore");
                });
            
            Target(Targets.BuildTests,
                DependsOn(Targets.BuildFluxzyCore),
                async () => {
                    await RunAsync("dotnet",
                        "build test/Fluxzy.Tests  --no-restore");
                });

            Target("tests",
                DependsOn(Targets.RestoreTests, Targets.BuildTests),
                async () => {
                    await RunAsync("dotnet",
                        "test test/Fluxzy.Tests --collect:\"XPlat Code Coverage\" --no-build");
                });
        }

        private static async Task CreateAndPushVersionedTag(string suffix)
        {
            var runningVersion = await GetRunningVersion() + suffix;
            var tagName = $"v{runningVersion}";

            await RunAsync("git", "config --global user.email \"admin@fluxzy.io\"");
            await RunAsync("git", "config --global user.name \"fluxzy-ci\"");

            await RunAsync("git", $"tag -a {tagName} -m \"Release {runningVersion}\"",
                handleExitCode: i => true);

            await RunAsync("git", $"push origin {tagName}", handleExitCode: i => true);
        }

        private static async Task Main(string[] args)
        {
            var (stdOut, _) = await ReadAsync("git", "branch --show-current");
            var currentBranch = stdOut.Trim();

            // Why there's no better way to do it?
            var current = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : OSPlatform.Windows;

            if (Directory.Exists("_npkgout")) {
                Directory.Delete("_npkgout", true);
            }
            
            Target(Targets.ValidateNugetToken, () => {
                EnvironmentHelper.GetEvOrFail("TOKEN_FOR_NUGET");
            });
            Target(Targets.ValidatePartnerSecret, () => {
                EnvironmentHelper.GetEvOrFail("PARTNER_SECRET");
            });

            Target(Targets.MustBeRelease,
                () => {
                    if (!currentBranch.StartsWith("release/") &&
                        Environment.GetEnvironmentVariable("SKIP_MANDATORY_RELEASE_BRANCH") != "1") {
                        throw new Exception($"Must be on release branch. Current branch is {currentBranch}");
                    }
                });

            Target(Targets.MustNotBeRelease,
                () => {
                    if (currentBranch.StartsWith("release/")){
                        throw new Exception($"Must be on non-release branch. Current branch is {currentBranch}");
                    }
                });

            AddBasicBuildTargets();

            Target(Targets.InstallTools,
                async () => {
                    await RunAsync("dotnet",
                        "tool install --global dotnet-script", handleExitCode: _ => true);

                    await RunAsync("dotnet",
                        "tool install --global nbgv", handleExitCode: _ => true);

                    await RunAsync("dotnet",
                        "tool install --global dotnet-project-licenses", handleExitCode: _ => true);
                });

            Target(Targets.FluxzyCoreCreatePackage,
                DependsOn(Targets.InstallTools),
                async () => {
                    await RunAsync("dotnet",
                        "build -c Release src/Fluxzy.Core");

                    await RunAsync("dotnet",
                        "pack -c Release src/Fluxzy.Core -o _npkgout");
                });

            Target(Targets.FluxzyCorePcapCreatePackage,
                DependsOn(Targets.FluxzyCoreCreatePackage),
                async () => {
                    await RunAsync("dotnet",
                        "build -c Release src/Fluxzy.Core.Pcap");

                    await RunAsync("dotnet",
                        "pack -c Release src/Fluxzy.Core.Pcap -o _npkgout");
                });

            Target(Targets.FluxzyPackageSign,
                DependsOn(Targets.FluxzyCorePcapCreatePackage),
                async () => { await SignHelper.SignPackages("_npkgout"); });

            Target(Targets.FluxzyPackagePushGithub,
                DependsOn(Targets.ValidateNugetToken, Targets.FluxzyPackageSign),
                async () => {
                    await RunAsync("dotnet",
                        $"nuget push *.nupkg --skip-duplicate --api-key { EnvironmentHelper.GetEvOrFail("TOKEN_FOR_NUGET")} " +
                        "--source https://nuget.pkg.github.com/haga-rak/index.json",
                        "_npkgout", true);
                });

            Target(Targets.FluxzyPackagePushPartner,
                DependsOn(Targets.ValidatePartnerSecret, Targets.FluxzyPackageSign),
                async () => {
                    await RunAsync("dotnet",
                        $"nuget push *.nupkg --skip-duplicate --api-key {EnvironmentHelper.GetEvOrFail("PARTNER_SECRET")} " +
                        "--source https://nuget.2befficient.io/v3/index.json",
                        "_npkgout", true);
                });

            Target(Targets.FluxzyPackagePushPublicInternal,
                DependsOn(Targets.FluxzyPackageSign),
                async () => {
                    var nugetOrgApiKey = EnvironmentHelper.GetEvOrFail("NUGET_ORG_API_KEY");

                    await RunAsync("dotnet",
                        $"nuget push *.nupkg --skip-duplicate --api-key {nugetOrgApiKey} " +
                        "--source https://api.nuget.org/v3/index.json",
                        "_npkgout", true);
                });

            Target(Targets.FluxzyCliPackageBuild,
                DependsOn(Targets.InstallTools, Targets.BuildFluxzyCore),
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

            Target(Targets.FluxzyCliPackageZip,
                DependsOn(Targets.FluxzyCliPackageSign),
                async () => {
                    var runningVersion = await GetRunningVersion();
                    Directory.CreateDirectory(".artefacts/final");

                    foreach (var runtimeIdentifier in TargetRuntimeIdentifiers[current]) {
                        var outDirectory = $".artefacts/{runtimeIdentifier}";

                        CompressionHelper.CreateCompressed(outDirectory,
                            $".artefacts/final/{GetFileName(runtimeIdentifier, runningVersion)}");
                    }
                });

            Target(Targets.FluxzyCliPublishInternal,
                DependsOn(Targets.FluxzyCliPackageZip),
                async () => {
                    var candidates = new DirectoryInfo(".artefacts/final/").EnumerateFiles("*.zip").ToList();

                    var uploadTasks = new List<Task>();

                    foreach (var candidate in candidates) {
                        uploadTasks.Add(Upload(candidate));
                    }

                    await Task.WhenAll(uploadTasks);
                });

            Target(Targets.FluxzyCliPackageSign,
                DependsOn(Targets.FluxzyCliPackageBuild),
                async () => {
                    if (current != OSPlatform.Windows) {
                        Console.WriteLine("Skipping signing for non-windows platform");

                        return;
                    }

                    if (BuildSettings.SkipSigning) {
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

                        signTasks.Add(SignHelper.SignCli(outDirectory, candidates));
                    }

                    await Task.WhenAll(signTasks);
                });

            Target(Targets.Docs, async () => {
                var docOutputPath = EnvironmentHelper.GetEvOrFail("DOCS_OUTPUT_PATH");

                await RunAsync("dotnet",
                    "run --project src/Fluxzy.Tools.DocGen",
                    configureEnvironment: env => env["DOCS_OUTPUT_PATH"] = docOutputPath,
                    noEcho: false);
            });

            Target(Targets.Default, () => Console.WriteLine("DefaultTarget is doing nothing"));

            // Validate current branch
            Target(Targets.ValidateMain, DependsOn("tests"));

            // Validate a pull request 
            Target(Targets.OnPullRequest, DependsOn("tests"));

            // Build local CLI packages signed 
            Target(Targets.FluxzyPublishNuget,
                DependsOn(Targets.InstallTools, Targets.FluxzyPackagePushGithub, Targets.FluxzyPackagePushPartner),
                async () => await CreateAndPushVersionedTag(""));

            Target(Targets.FluxzyPublishNugetPublic,
                DependsOn(Targets.MustBeRelease, Targets.InstallTools, Targets.FluxzyPublishNuget, Targets.FluxzyPackagePushPublicInternal),
                async () => await CreateAndPushVersionedTag(""));

            Target(Targets.FluxzyPublishNugetPublicPreRelease,
                DependsOn(Targets.MustNotBeRelease, Targets.InstallTools, Targets.FluxzyPublishNuget, Targets.FluxzyPackagePushPublicInternal),
                async () => await CreateAndPushVersionedTag("-pre"));

            Target(Targets.FluxzyPublishNugetPublicWithNote,
                DependsOn(Targets.MustBeRelease, Targets.FluxzyPublishNugetPublic),
                async () => {
                    var publishHelper = await
                        GhPublishHelper.Create(EnvironmentHelper.GetEvOrFail("GH_RELEASE_TOKEN"),
                            EnvironmentHelper.GetEvOrFail("REPOSITORY_OWNER"),
                            EnvironmentHelper.GetEvOrFail("REPOSITORY_NAME"));

                    var tag = "v" + await GetRunningVersion();
                    await publishHelper.Publish(tag);
                });

            Target(Targets.FluxzyCliFullPackage, DependsOn(Targets.FluxzyCliPackageZip));

            // Build local CLI packages signed 
            Target(Targets.FluxzyCliPublish, DependsOn(Targets.InstallTools, Targets.FluxzyCliPublishInternal),
                async () => await CreateAndPushVersionedTag("-cli"));

            // Build local CLI packages signed
            Target(Targets.FluxzyCliPublishWithNote, DependsOn(Targets.FluxzyCliPublish),
                async () => {
                    var publishHelper = await
                        GhPublishHelper.Create(EnvironmentHelper.GetEvOrFail("GH_RELEASE_TOKEN"),
                            EnvironmentHelper.GetEvOrFail("REPOSITORY_OWNER"),
                            EnvironmentHelper.GetEvOrFail("REPOSITORY_NAME"));

                    // Upload both .zip and .tar.gz assets
                    var assets = new DirectoryInfo(".artefacts/final/")
                        .EnumerateFiles()
                        .Where(f => f.Extension == ".zip" || f.Name.EndsWith(".tar.gz"));

                    var tag = "v" + await GetRunningVersion();

                    await publishHelper.AddAssets(tag, assets);
                });

            Target(Targets.FluxzyCliPublishDocker,
                DependsOn(Targets.InstallTools, Targets.MustBeRelease),
                async () => {
                    var shortVersion = await GetRunningVersionShort();
                    await DockerHelper.BuildDockerImage(".", shortVersion);
                    await DockerHelper.PushDockerImage(".", shortVersion);
                });

            Target(Targets.FluxzyStressTest,
                DependsOn(),
                async () => {
                    await FloodyBenchmark.Run(new FloodyBenchmarkSetting());
                });

            Target(Targets.FluxzyStressTestPlain,
                DependsOn(),
                async () => {
                    await FloodyBenchmark.Run(new FloodyBenchmarkSetting() {
                        Plain = true
                    });
                });

            await RunTargetsAndExitAsync(args, ex => ex is ExitCodeException);
        }
    }

}
