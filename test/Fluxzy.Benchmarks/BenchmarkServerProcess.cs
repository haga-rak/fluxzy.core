using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Benchmarks;

/// <summary>
///     Manages the lifecycle of the external Fluxzy.Benchmarks.Server process.
/// </summary>
public class BenchmarkServerProcess : IAsyncDisposable
{
    private Process? _process;

    public int Port { get; private set; }

    public string BaseUrl => $"https://localhost:{Port}";

    public async Task StartAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);

        var serverProjectPath = ResolveServerProjectPath();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{serverProjectPath}\" -c Release --no-build",
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = psi };

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null && e.Data.StartsWith("LISTENING:"))
            {
                if (int.TryParse(e.Data.AsSpan("LISTENING:".Length), out var port))
                    tcs.TrySetResult(port);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();

        using var cts = new CancellationTokenSource(timeout.Value);
        cts.Token.Register(() => tcs.TrySetException(
            new TimeoutException("Benchmark server did not start in time")));

        Port = await tcs.Task;
    }

    public async ValueTask DisposeAsync()
    {
        if (_process == null)
            return;

        try
        {
            // Signal the server to shut down by closing stdin
            _process.StandardInput.Close();
            await _process.WaitForExitAsync(new CancellationTokenSource(5000).Token);
        }
        catch
        {
            _process.Kill(entireProcessTree: true);
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private static string ResolveServerProjectPath()
    {
        // Walk up from the benchmark assembly location to find the solution root
        var dir = AppContext.BaseDirectory;

        while (dir != null)
        {
            var slnx = Path.Combine(dir, "fluxzy.core.slnx");

            if (File.Exists(slnx))
                return Path.Combine(dir, "test", "Fluxzy.Benchmarks.Server");

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            "Could not find solution root (fluxzy.core.slnx). " +
            "Make sure the benchmark is run from within the repository.");
    }
}
