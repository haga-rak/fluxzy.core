// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Diagnostics;
using System.Text;
using Fluxzy.Interop.Pcap.Pcapng;
using Fluxzy.Readers;
using SharpPcap;

namespace Fluxzy.Desktop.Services.Ui
{
    public class FileExecutionManager
    {
        public async Task<string?> GetNssKey(int connectionId, IArchiveReader archiveReader)
        {
            if (!(archiveReader is DirectoryArchiveReader directoryArchiveReader)) {
                return null;  
            }

            await using var keyStream = directoryArchiveReader.GetRawCaptureKeyStream(connectionId);

            if (keyStream == null)
                return null;

            using var reader = new StreamReader(keyStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

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
        public async Task<bool> OpenPcapWithKey(int connectionId, IArchiveReader archiveReader)
        {
            if (!(archiveReader is DirectoryArchiveReader directoryArchiveReader)) {
                return false;  
            }

            await using var keyStream = directoryArchiveReader.GetRawCaptureKeyStream(connectionId);

            if (keyStream == null)
                return false;

            using var reader = new StreamReader(keyStream, Encoding.UTF8);
            var nssKey =  await reader.ReadToEndAsync();

            var pcapStream = directoryArchiveReader.GetRawCaptureStream(connectionId);

            if (pcapStream == null)
                return false;

            var tempFile = Path.Combine(Environment.ExpandEnvironmentVariables("%appdata%/fluxzy/temp/export"));

            Directory.CreateDirectory(tempFile);

            tempFile = Path.Combine(tempFile, $"{connectionId}.pcapng");

            using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                using var tempStream = PcapngNssUtils.GetNssIncludedStream(pcapStream, nssKey);

                await tempStream.CopyToAsync(fileStream);
            }
            
            return ShellExecutor(tempFile);
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