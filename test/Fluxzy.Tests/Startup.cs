// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core.Pcap.Cli.Clients;
using Fluxzy.Tests._Files;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Fluxzy.Tests.Startup", "Fluxzy.Tests")]

namespace Fluxzy.Tests
{
    public class Startup : XunitTestFramework
    {
        public static string DirectoryName { get; set; } = string.Empty;

        public static string DefaultMergeArchivePcapHash { get; } =
            "5cf21b2c48ae241f46ddebf30fca6de2f757df52d00d9cf939b750f0296d33b8";

        public static string DefaultArchiveHash { get; } =
            "b74d2bb7de2579a46bd782d3133c7acce8353352fd22fb14067e264f6ba93540";

        public Startup(IMessageSink messageSink)
            : base(messageSink)
        {
            Environment.SetEnvironmentVariable("SSLKEYLOGFILE", @"d:\key.log");

            foreach (var fileSystemInfo in new DirectoryInfo(".").EnumerateFileSystemInfos().ToList())
            {
                if (fileSystemInfo is DirectoryInfo directory && Guid.TryParse(directory.Name, out _))
                {
                    directory.Delete(true);
                }

                if (fileSystemInfo is FileInfo file &&
                    (file.Name.EndsWith(".yml")
                     || file.Name.EndsWith(".yaml")
                     || file.Name.EndsWith(".temp")
                     ))
                {
                    file.Delete();
                }
            }

            InstallCertificate();

            DirectoryName = EmptyDirectory("static_website_dir");

            EmptyDirectory("Drop");

            ExtractDirectory(StorageContext.static_ws, DirectoryName);
            ExtractDirectory(File.ReadAllBytes("_Files/Archives/pink-floyd.fxzy"), ".artefacts/tests/pink-floyd");

            CertificateContext.InstallDefaultCertificate();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                _ = Task.Run(async () => 
                            await Utility.AcquireCapabilities(new FileInfo("fluxzynetcap").FullName))
                        .GetAwaiter().GetResult();
            }
        }

        private static void ExtractDirectory(byte [] binary, string directoryName)
        {
            using var zipArchive = new ZipArchive(new MemoryStream(binary), ZipArchiveMode.Read);
            zipArchive.ExtractToDirectory(directoryName, true);
        }

        private void InstallCertificate()
        {
            DefaultCertificateAuthorityManager authorityManager = new();
            authorityManager.CheckAndInstallCertificate(FluxzySetting.CreateDefault(IPAddress.Loopback, 4444).CaCertificate.GetX509Certificate());
        }

        public new void Dispose()
        {
            // Place tear down code here
            base.Dispose();
        }

        private static string EmptyDirectory(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, true);
            }

            Directory.CreateDirectory(directoryName);

            return directoryName;
        }

    }
}
