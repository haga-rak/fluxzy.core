// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionClientCertificateCustomValidation : WithRuleOptionBase
    {
        [TheoryIfEnvVarExists("PRIVATE_CLIENT_CERTIFICATE_TEST_TARGETS")]
        [MemberData(nameof(GetPrivateClientCertificateTestTargetParams))]
        public async Task Validate(string url, string pfxPath, string pfxPassword, string? forcedIp, string tlsVersion)
        {
            // Arrange 
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, url);
            
            var yamlContent = $"""
                              rules:
                                - filter:
                                    typeKind: AnyFilter
                                  actions :
                                    - typeKind: SetClientCertificateAction
                                      clientCertificate:
                                        pkcs12File: {pfxPath}
                                        pkcs12Password: {pfxPassword}
                                        retrieveMode: FromPkcs12
                                    - typeKind: forceTlsVersionAction
                                      sslProtocols: {tlsVersion}
                              """;

            if (forcedIp != null) {
                var extraSpoof = $"""
                                  
                                    - filter:
                                        typeKind: AnyFilter
                                      action :
                                        typeKind: SpoofDnsAction
                                        remoteHostIp: {forcedIp}
                                  """;

                yamlContent += extraSpoof;
            }

            using var response = await Exec(yamlContent, requestMessage, useBouncyCastle: true);
            Assert.NotEqual(528, (int)response.StatusCode);
        }

        public static IEnumerable<object?[]> GetPrivateClientCertificateTestTargetParams()
        {
            var tlsVersions = new string[] { "none" };
            var targets = PrivateClientCertificateTestTarget.ReadFromEnvironment();

            foreach (var target in targets)
            {
                foreach (var tlsVersion in tlsVersions)
                {
                    yield return new object?[] { target.Url, target.PfxPath, target.PfxPassword, target.ForcedIp, tlsVersion };
                }
                
            }
        }
    }

    public class PrivateClientCertificateTestTarget
    {
        public PrivateClientCertificateTestTarget(string url, string pfxPath, string pfxPassword, string? forcedIp)
        {
            Url = url;
            PfxPath = pfxPath;
            PfxPassword = pfxPassword;
            ForcedIp = forcedIp;
        }

        public string Url { get; }

        public string PfxPath { get; }

        public string PfxPassword { get; }

        public string? ForcedIp { get; }

        public static IReadOnlyCollection<PrivateClientCertificateTestTarget> ReadFromEnvironment()
        {
            var envRaw = Environment.GetEnvironmentVariable("PRIVATE_CLIENT_CERTIFICATE_TEST_TARGETS")
                ?? string.Empty;

            var lineRawTab = envRaw.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<PrivateClientCertificateTestTarget>();


            foreach (var lineRaw in lineRawTab) {
                var parts = lineRaw.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                {
                    throw new InvalidOperationException("Invalid format");
                }

                var forcedIp = (string?) null ;

                if (parts.Length == 4)
                {
                    forcedIp = parts[3];
                }

                result.Add(new PrivateClientCertificateTestTarget(parts[0], parts[1], parts[2], forcedIp));
            }

            return result;

        }
    }

    public class TheoryIfEnvVarExistsAttribute : TheoryAttribute
    {
        private readonly string _envVarName;

        public TheoryIfEnvVarExistsAttribute(string envVarName)
        {
            _envVarName = envVarName;
        }

        public override string? Skip {
            get
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(_envVarName)))
                {
                    return $"Environment variable {_envVarName} is missing. Test skipped";
                }

                return null;
            }
        }
    }
}
