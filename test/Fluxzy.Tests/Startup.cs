// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.IO.Compression;
using System.Net;
using Fluxzy.Certificates;
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

        public Startup(IMessageSink messageSink)
            : base(messageSink)
        {
            InstallCertificate();

            DirectoryName = EmptyDirectory("static_website_dir");

            ExtractDirectory(StorageContext.static_ws, DirectoryName);
            ExtractDirectory(File.ReadAllBytes("_Files/Archives/pink-floyd.fxzy"), ".artefacts/tests/pink-floyd");

            // Environment.SetEnvironmentVariable("Fluxzy_EnableNetworkFileDump", "true");
            // Environment.SetEnvironmentVariable("Fluxzy_EnableWindowSizeTrace", "true");

            // Environment.SetEnvironmentVariable("EnableH1Tracing", "true");
            // Environment.SetEnvironmentVariable("EnableH2Tracing", "true");

            // Environment.SetEnvironmentVariable("EnableH2TracingFilterHosts",
            //    "2befficient.fr;smartizy.com; discord.com; facebook.com; google.com");
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
