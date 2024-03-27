// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using static SimpleExec.Command;

namespace Fluxzy.Build
{
    internal static class DockerHelper
    {
        public static async Task BuildDockerImage(string workingDirectory, string shortVersion)
        {
            await RunAsync("docker", $"build -t fluxzy/fluxzy -t fluxzy/fluxzy:{shortVersion} -f docker/Dockerfile.alpine-x64 .",
                workingDirectory: workingDirectory, noEcho: false);
        }

        public static async Task PushDockerImage(string workingDirectory, string shortVersion)
        {
            await RunAsync("docker", $"push fluxzy/fluxzy:{shortVersion}",
                workingDirectory: workingDirectory, noEcho: false);

            await RunAsync("docker", $"push fluxzy/fluxzy:latest",
                workingDirectory: workingDirectory, noEcho: false);
        }
    }
}
