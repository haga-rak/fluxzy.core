// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Diagnostics;
using Fluxzy.Readers;

namespace Fluxzy.Desktop.Services.Ui
{
    public class FileExecutionManager
    {
        public async Task<bool> OpenPcap(int connectionId, IArchiveReader archiveReader)
        {
            if (!(archiveReader is DirectoryArchiveReader directoryArchiveReader)) {
                return false;  
            }

            var fileName = directoryArchiveReader.GetRawCaptureFile(connectionId);

            if (fileName == null)
                return false;

            return ShellExecutor(fileName);
        }

        private static bool ShellExecutor(string fileName)
        {
            try {
                var processStartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    UseShellExecute = true,
                };

                var process = new Process {
                    StartInfo = processStartInfo
                };

                process.Start();
                return true;
            }
            catch {
                return false;
            }
        }
    }
}