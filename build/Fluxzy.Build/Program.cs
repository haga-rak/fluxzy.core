using Bullseye;
using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace Fluxzy.Build
{
    /// <summary>
    /// This program contains the main pipelines for buliding the project.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Obtains GetEvOrFail or throw exception if not found.
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        static string GetEvOrFail(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName) ?? 
                   throw new System.Exception($"GetEvOrFail {variableName} not found");
        }

        static async Task Main(string[] args)
        {
            var (stdOut, stdErr) = await ReadAsync("git", "branch --show-current");
            var currentBranch = stdOut.Trim();

            var privateNugetToken = GetEvOrFail("TOKEN_FOR_NUGET");

            Target("must-be-release", 
                () => {
                    if (!currentBranch.StartsWith("release/")) {
                        throw new Exception($"Must be on release branch. Current branch is {currentBranch}");
                    }
                });

            Target("add-nuget-source", 
                async () => {
                    await RunAsync("dotnet",
                        "add source https://nuget.pkg.github.com/haga-rak/index.json " +
                        $"-n nuget-fluxy -u haga-rak -p {privateNugetToken}", handleExitCode: _ => true);
                });

            Target("restore-tests", 
                DependsOn("add-nuget-source"),
                async () =>
                {
                    await RunAsync("dotnet",
                        "restore test/Fluxzy.Tests");
                });

            Target("restore-fluxzy-core",
                DependsOn("add-nuget-source"),
                async () =>
                {
                    await RunAsync("dotnet",
                        "restore src/Fluxzy.Core");
                });

            Target("build-fluxzy-core",
                DependsOn("restore-fluxzy-core"),
                async () =>
                {
                    await RunAsync("dotnet",
                        "build src/Fluxzy.Core  --no-restore");
                });

            Target("tests",
                DependsOn("restore-tests", "build-fluxzy-core"),
                async () =>
                {
                    await RunAsync("dotnet",
                        "test test/Fluxzy.Tests -e EnableDumpStackTraceOn502=true");
                });


            Target("validate-main", DependsOn("tests"));


            Target("default", DependsOn("build-fluxzy-core"));
            
            await RunTargetsAndExitAsync(args, ex => ex is SimpleExec.ExitCodeException);
        }
    }
}