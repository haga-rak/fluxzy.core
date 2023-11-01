// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO.Compression;
using System.Runtime.InteropServices;
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
                   throw new Exception($"GetEvOrFail {variableName} not found");
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

        private static async Task Main(string[] args)
        {
            var (stdOut, _) = await ReadAsync("git", "branch --show-current");
            var currentBranch = stdOut.Trim();
            var privateNugetToken = GetEvOrFail("TOKEN_FOR_NUGET");

            // Why there's no better way to do it?
            var current = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : OSPlatform.Windows;

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

            Target("fluxzy-cli-package-build",
                DependsOn("install-tools", "build-fluxzy-core"),
                async () => {
                    if (Directory.Exists(".artefacts"))
                        Directory.Delete(".artefacts", true);
                    
                    foreach (var runtimeIdentifier in TargetRuntimeIdentifiers[current]) {
                        var outDirectory = $".artefacts/{runtimeIdentifier}";

                        await RunAsync("dotnet",
                            $"publish --sc true -c Release -r {runtimeIdentifier} \"/p:DebugType=None\" \"/p:DebugSymbols=false\" -o {outDirectory} \"./src/Fluxzy");
                    }
                });

            Target("fluxzy-cli-package-zip",
                DependsOn("fluxzy-cli-package-build"),
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

            Target("default", () => Console.WriteLine("No default target"));

            Target("validate-main", DependsOn("tests"));
            Target("on-pull-request", DependsOn("tests"));

            await RunTargetsAndExitAsync(args, ex => ex is ExitCodeException);
        }

        private static void AddBasicBuildTargets(string privateNugetToken)
        {
            Target("add-nuget-source",
                async () => {
                    await RunAsync("dotnet",
                        "nuget add source https://nuget.pkg.github.com/haga-rak/index.json " +
                        $"-n nuget-fluxy -u haga-rak -p {privateNugetToken}", handleExitCode: _ => true, noEcho:true);
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
    }
}
