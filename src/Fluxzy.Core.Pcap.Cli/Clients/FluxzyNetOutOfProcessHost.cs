// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Diagnostics;
using System.Runtime.InteropServices;
using Fluxzy.Misc;

namespace Fluxzy.Core.Pcap.Cli.Clients
{
    public class FluxzyNetOutOfProcessHost : IOutOfProcessHost
    {
        private Process? _process;

        public int Port { get; private set; }

        public async Task<bool> Start()
        {
            var currentPid = Process.GetCurrentProcess().Id;
            var commandName = new FileInfo(typeof(Program).Assembly.Location).FullName;

            if (commandName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
                commandName = commandName.Substring(0, commandName.Length - 4);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    commandName += ".exe"; // TODO : find a more elegant trick than this 
            }

            _process = await ProcessUtils.RunElevatedAsync(commandName, new[] { $"{currentPid}" }, true,
                "Fluxzy need to acquire privilege for capturing raw packet");


            if (_process == null) {
                // Log "Cannot run process as sudo"
                FaultedOrDisposed = true;

                return false;
            }

            try {
                _process.Exited += ProcessOnExited;
                _process.EnableRaisingEvents = true;

                var nextLine = await _process.StandardOutput.ReadLineAsync()

                                             // We wait 5s for the the process to be ready
                                             .WaitAsync(TimeSpan.FromSeconds(300));

                if (nextLine == null || !int.TryParse(nextLine, out var port))
                    return false; // Did not receive port number

                Port = port;


                return true;
            }
            catch (OperationCanceledException) {
                // Timeout probably expired 

                return false;
            }

            // next line should be the 
        }

        /// <summary>
        ///     Shall be port number
        /// </summary>
        public object Payload => Port;

        public bool FaultedOrDisposed { get; private set; }

        public ICaptureContext? Context { get; set; }

        public async ValueTask DisposeAsync()
        {
            if (_process != null) {
                try {
                    await _process.StandardInput.WriteLineAsync("exit");
                    _process.StandardInput.Close();
                    await _process.WaitForExitAsync();
                }
                catch {
                    // Ignore killing failure 
                }
            }

            _process?.Dispose();
        }

        private void ProcessOnExited(object? sender, EventArgs e)
        {
            FaultedOrDisposed = true;

            if (_process != null)
                _process.Exited -= ProcessOnExited; // Detach process
        }

        public void Dispose()
        {
        }
    }
}
