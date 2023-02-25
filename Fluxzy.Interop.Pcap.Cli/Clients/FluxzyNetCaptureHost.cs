using System.Diagnostics;
using System.Runtime.InteropServices;
using Fluxzy.Core;
using Fluxzy.Misc;

namespace Fluxzy.Interop.Pcap.Cli.Clients
{
    public class FluxzyNetCaptureHost : ICaptureHost
    {
        private Process? _process;

        public async Task<bool> Start()
        {
            // Check if process is run 
            // run the external process as sudo 
            // save the state of sudo acquisition 

            var currentPid = Process.GetCurrentProcess().Id; 
            var commandName = new FileInfo(typeof(Program).Assembly.Location).FullName;

            if (commandName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
                commandName = commandName.Substring(0, commandName.Length - 4);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    commandName += ".exe"; // TODO : find a more elegant trick than this 
                }
            }

            _process = ProcessUtils.RunElevated(commandName, new [] { $"{currentPid}" }, true);

            if (_process == null) {
                // Log "Cannot run process as sudo"
                FaultedOrDisposed = true;
                return false ; 
            }

            try
            {
                _process.Exited += ProcessOnExited;

                var nextLine = await _process.StandardOutput.ReadLineAsync()
                                        // A global timeout of 30s 
                                        // the user may be prompted to enter the password so it takes that time  limit
                                      .WaitAsync(TimeSpan.FromSeconds(30));

                if (nextLine == null || !int.TryParse(nextLine, out var port)) {
                    return false; // Did not receive port number
                }

                Port = port;


                return true; 
            }
            catch (OperationCanceledException) {
                // Timeout probably expired 

                return false; 
            }


            // next line should be the 
        }

        private void ProcessOnExited(object? sender, EventArgs e)
        {
            FaultedOrDisposed = true;

            if (_process != null) {
                _process.Exited -= ProcessOnExited; // Detach process
            }
        }

        public int Port { get; private set; }



        /// <summary>
        /// Shall be port number 
        /// </summary>
        public object Context => Port; 


        public bool FaultedOrDisposed { get; private set; }

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            if (_process != null)
            {
                try
                {
                    _process.StandardInput.WriteLine("exit");
                    _process.StandardInput.Close();
                    await _process.WaitForExitAsync();
                }
                catch
                {
                    // Ignore killing failure 
                }
            }

            _process?.Dispose();
        }
    }
}