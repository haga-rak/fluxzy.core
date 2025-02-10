// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using SimpleExec;
using System.Net.Sockets;
using System.Net;

namespace Fluxzy.Build
{
    internal static class FloodyBenchmark
    {
        internal static int GetAFreePort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var localEp = new IPEndPoint(IPAddress.Loopback, 0);
            socket.Bind(localEp);
            localEp = (IPEndPoint) socket.LocalEndPoint!;
            return localEp.Port;;
        }

        public static async Task Run(FloodyBenchmarkSetting setting)
        {
            if (!Directory.Exists("src/Fluxzy"))
                throw new DirectoryNotFoundException("Must be run from the root of the repository");
            
            await Command.RunAsync("dotnet",
                $"publish --sc true -c Release src/Fluxzy -o \"{setting.FluxzyOutDirectory}\"",
                echoPrefix: "PUBLISH FLUXZY");

            var cloneDirectory = setting.FloodyCloneDirectory;

            if (!Directory.Exists(Path.Combine(cloneDirectory, ".git")))
            {
                await Command.RunAsync("git", $"clone https://github.com/haga-rak/floody.git \"{cloneDirectory}\"",
                    echoPrefix: "CLONE FLOODY");
            }
            else {
                await Command.RunAsync("git", $"fetch",
                    workingDirectory: Path.Combine(cloneDirectory),
                    echoPrefix: "FETCH FLOODY");

                await Command.RunAsync("git", $"reset --hard origin",
                    workingDirectory: Path.Combine(cloneDirectory),
                    echoPrefix: "RESET FLOODY");
            }

            using var haltSource = new CancellationTokenSource();
            var token = haltSource.Token;

            var listenPort = GetAFreePort();

            var fluxzyRunPromise = Command.RunAsync(
                Path.Combine(setting.FluxzyOutDirectory, "fluxzy"), $"start -k -l " +
                                                                    $"127.0.0.1:{listenPort} " +
                                                                    $"{setting.FluxzyExtraSettings}",
                workingDirectory: setting.FluxzyOutDirectory,
                echoPrefix: "FLUXZY RUN", cancellationToken: token);

            await Task.Delay(500, token);

            var testTarget = setting.Plain ? "test-http" : "test-https";

            try {
                await Command.RunAsync("dotnet", 
                    $"run --project build/build.csproj -- {testTarget} " +
                    $"\"floody-options:-d {setting.Duration} -w {setting.WarmupDuration} -x 127.0.0.1:{listenPort}\"",
                    workingDirectory: setting.FloodyCloneDirectory,
                    echoPrefix: "FLUXZY RUN", cancellationToken: token);
            }
            finally
            {
                await haltSource.CancelAsync();
                Console.WriteLine("Waiting for fluxzy to stop...");
            }

            try {
                await fluxzyRunPromise;
            }
            catch (Exception e) {
                // Ignore operation cancelleted 
            }
        }
    }

    public class FloodyBenchmarkSetting
    {
        public int Duration { get; set; } = 15; 

        public int WarmupDuration { get; set; } = 5;

        public string FluxzyExtraSettings { get; set; } 
          = Environment.GetEnvironmentVariable("FLUXZY_EXTRA_SETTINGS") ?? string.Empty;

        public bool Plain { get; set; }

        public string FloodyCloneDirectory { get; set; }
          = Environment.GetEnvironmentVariable("FLOODY_CLONE_DIRECTORY") ?? GetDefaultFloodyCloneDirectory();

        public string FluxzyOutDirectory { get; set; }
          = Environment.GetEnvironmentVariable("FLUXZY_OUT_DIRECTORY") ?? GetFluxzyOutputDirectory();
        
        private static string GetDefaultFloodyCloneDirectory()
        {
            var fullPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "floody", "map");

            if (Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }

        private static string GetFluxzyOutputDirectory()
        {
            var fullPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "floody", "fluxzy");

            if (Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }
    }
}
