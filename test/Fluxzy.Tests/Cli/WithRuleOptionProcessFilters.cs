// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionProcessFilters : IAsyncDisposable
    {
        private ProxyInstance? _fluxzyInstance;
        private string? _ruleFile;

        public async ValueTask DisposeAsync()
        {
            if (_ruleFile != null && File.Exists(_ruleFile))
                File.Delete(_ruleFile);

            if (_fluxzyInstance != null)
                await _fluxzyInstance.DisposeAsync();
        }

        private async Task<ProxyInstance> StartProxy(string yamlContent)
        {
            var commandLine = "start -l 127.0.0.1:0 --no-cert-cache --enable-process-tracking";
            var uniqueIdentifier = Guid.NewGuid().ToString();

            _ruleFile = $"{uniqueIdentifier}.yml";
            await File.WriteAllTextAsync(_ruleFile, yamlContent);

            commandLine += $" -r {_ruleFile}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);
            return await commandLineHost.Run();
        }

        private static async Task<(int exitCode, string output)> RunCurl(int proxyPort, string url, int timeoutSeconds = 30)
        {
            var curlPath = GetCurlPath();

            var psi = new ProcessStartInfo
            {
                FileName = curlPath,
                Arguments = $"-s -k -x http://127.0.0.1:{proxyPort} -D - \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                throw new TimeoutException("Curl process timed out");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            return (process.ExitCode, output);
        }

        private static string GetCurlPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "curl.exe";

            return "curl";
        }

        [Fact]
        public async Task ProcessNameFilter_MatchesCurl()
        {
            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: ProcessNameFilter
                                  processNames:
                                    - curl
                                action:
                                  typeKind: AddResponseHeaderAction
                                  headerName: x-process-matched
                                  headerValue: "true"
                              """;

            _fluxzyInstance = await StartProxy(yamlContent);

            var (exitCode, output) = await RunCurl(_fluxzyInstance.ListenPort,
                $"{TestConstants.PlainHttp11}/global-health-check");

            Assert.Equal(0, exitCode);
            Assert.Contains("x-process-matched: true", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessNameFilter_DoesNotMatchWrongName()
        {
            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: ProcessNameFilter
                                  processNames:
                                    - nonexistent-process
                                action:
                                  typeKind: AddResponseHeaderAction
                                  headerName: x-process-matched
                                  headerValue: "true"
                              """;

            _fluxzyInstance = await StartProxy(yamlContent);

            var (exitCode, output) = await RunCurl(_fluxzyInstance.ListenPort,
                $"{TestConstants.PlainHttp11}/global-health-check");

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("x-process-matched", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessNameFilter_MatchesWithExeExtension()
        {
            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: ProcessNameFilter
                                  processNames:
                                    - curl.exe
                                action:
                                  typeKind: AddResponseHeaderAction
                                  headerName: x-process-matched
                                  headerValue: "true"
                              """;

            _fluxzyInstance = await StartProxy(yamlContent);

            var (exitCode, output) = await RunCurl(_fluxzyInstance.ListenPort,
                $"{TestConstants.PlainHttp11}/global-health-check");

            Assert.Equal(0, exitCode);
            Assert.Contains("x-process-matched: true", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessNameFilter_MatchesMultipleNames()
        {
            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: ProcessNameFilter
                                  processNames:
                                    - wget
                                    - curl
                                    - httpie
                                action:
                                  typeKind: AddResponseHeaderAction
                                  headerName: x-process-matched
                                  headerValue: "true"
                              """;

            _fluxzyInstance = await StartProxy(yamlContent);

            var (exitCode, output) = await RunCurl(_fluxzyInstance.ListenPort,
                $"{TestConstants.PlainHttp11}/global-health-check");

            Assert.Equal(0, exitCode);
            Assert.Contains("x-process-matched: true", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessIdFilter_MatchesCurlProcess()
        {
            var curlPath = GetCurlPath();
            var curlProcessId = 0;
            var yamlPlaceholder = "PROCESS_ID_PLACEHOLDER";

            var yamlTemplate = $"""
                                rules:
                                - filter:
                                    typeKind: ProcessIdFilter
                                    processId: {yamlPlaceholder}
                                  action:
                                    typeKind: AddResponseHeaderAction
                                    headerName: x-process-matched
                                    headerValue: "true"
                                """;

            var commandLine = "start -l 127.0.0.1:0 --no-cert-cache --enable-process-tracking";
            var uniqueIdentifier = Guid.NewGuid().ToString();
            _ruleFile = $"{uniqueIdentifier}.yml";

            await File.WriteAllTextAsync(_ruleFile, yamlTemplate.Replace(yamlPlaceholder, "99999999"));
            commandLine += $" -r {_ruleFile}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);
            _fluxzyInstance = await commandLineHost.Run();

            var psi = new ProcessStartInfo
            {
                FileName = curlPath,
                Arguments = $"-s -k -x http://127.0.0.1:{_fluxzyInstance.ListenPort} -D - \"{TestConstants.PlainHttp11}/global-health-check\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            curlProcessId = process.Id;

            await _fluxzyInstance.DisposeAsync();

            var yamlContent = yamlTemplate.Replace(yamlPlaceholder, curlProcessId.ToString());
            await File.WriteAllTextAsync(_ruleFile, yamlContent);

            commandLineHost = new FluxzyCommandLineHost($"start -l 127.0.0.1:0 --no-cert-cache --enable-process-tracking -r {_ruleFile}");
            _fluxzyInstance = await commandLineHost.Run();

            psi.Arguments = $"-s -k -x http://127.0.0.1:{_fluxzyInstance.ListenPort} -D - \"{TestConstants.PlainHttp11}/global-health-check\"";

            using var process2 = new Process { StartInfo = psi };
            process2.Start();

            var sameProcessId = process2.Id == curlProcessId;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await process.WaitForExitAsync(cts.Token);
            await process2.WaitForExitAsync(cts.Token);

            var output = await process2.StandardOutput.ReadToEndAsync();

            if (sameProcessId)
                Assert.Contains("x-process-matched: true", output, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessIdFilter_DoesNotMatchWrongPid()
        {
            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: ProcessIdFilter
                                  processId: 99999999
                                action:
                                  typeKind: AddResponseHeaderAction
                                  headerName: x-process-matched
                                  headerValue: "true"
                              """;

            _fluxzyInstance = await StartProxy(yamlContent);

            var (exitCode, output) = await RunCurl(_fluxzyInstance.ListenPort,
                $"{TestConstants.PlainHttp11}/global-health-check");

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("x-process-matched", output, StringComparison.OrdinalIgnoreCase);
        }
    }
}
