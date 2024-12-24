// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluxzy.Tests._Fixtures;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Clients.Ssl;
using Fluxzy.Rules.Actions;
using Xunit;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Fluxzy.Tests
{
    public class Ja3FingerPrintTests
    {
        [Theory]
        [InlineData("769,49195-49199-49196-49200-52393-52392-52244-52243-49161-49171-49162-49172-156-157-47-53-10,65281-0-23-35-13-5-18-16-11-10-21,29-23-24,0")]
        [InlineData("772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,65281-65037-51-45-35-17513-10-11-43-27-5-13-23-18-0-16,4588-29-23-24,0")]
        [InlineData("772,,,4588-29-23-24,")]
        public void Format_Parse_Unparse(string originalFingerPrint)
        {
            var fingerPrint = Ja3FingerPrint.Parse(originalFingerPrint);
            var value = fingerPrint.ToString();

            Assert.Equal(originalFingerPrint, value);
        }

        [Theory]
        [MemberData(nameof(Ja3FingerPrintTestLoader.LoadTestDataAsObject), MemberType = typeof(Ja3FingerPrintTestLoader))]
        public async Task Validate(string _, string clientName, string expectedJa3)
        {
            var testUrl = "https://check.ja3.zone/";

            await using var proxy = new AddHocConfigurableProxy(1, 10, 
                configureSetting : setting => {
                setting.UseBouncyCastleSslEngine();
                setting.AddAlterationRulesForAny(new SetJa3FingerPrintAction(expectedJa3));
            });

            using var httpClient = proxy.RunAndGetClient();
            using var response = await httpClient.GetAsync(testUrl);

            response.EnsureSuccessStatusCode();
            
            var responseString = await response.Content.ReadAsStringAsync();

            var ja3Response = JsonSerializer.Deserialize<Ja3FingerPrintResponse>(responseString);
            
            Assert.NotNull(ja3Response);
            Assert.Equal(expectedJa3, ja3Response.NormalizedFingerPrint);
        }

        [Theory]
        [MemberData(nameof(Ja3FingerPrintTestLoader.LoadTestDataAsObject), MemberType = typeof(Ja3FingerPrintTestLoader))]
        public async Task ConnectOnly(string host, string clientName, string expectedJa3)
        {
            var lastStatus = 0; 

            for (int i = 0; i < 1 ; i ++) {

                var testUrl = host;
                await using var proxy = new AddHocConfigurableProxy(1, 10,
                    configureSetting: setting => {
                        setting.UseBouncyCastleSslEngine();
                        setting.AddAlterationRulesForAny(new SetJa3FingerPrintAction(expectedJa3));

                        setting.AddAlterationRules(new SpoofDnsAction()
                        {
                            RemoteHostIp = "142.250.178.132"
                        }, new HostFilter("google.com", StringSelectorOperation.EndsWith));
                    });

                using var httpClient = proxy.RunAndGetClient();
                using var response = await httpClient.GetAsync(testUrl);

                lastStatus = (int)response.StatusCode;
            }


            Assert.NotEqual(528, (int)lastStatus);
        }

        private static void CleanupDirectory(string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);

            if (!directoryInfo.Exists)
            {
                return;
            }

            foreach (var f in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).ToList()) 
            {
                try {
                    f.Delete();
                }
                catch { 
                    // Ignore
                }
            }
        }
    }

    public static class Ja3FingerPrintTestLoader
    {
        public static IEnumerable<(String FriendlyName, Ja3FingerPrint FingerPrint)> LoadTestData()
        {
            var testFile = "_Files/Ja3/fingerprints.txt";

            var lines = File.ReadAllLines(testFile);

            foreach (var line in lines) {

                if (line.TrimStart(' ').StartsWith("//"))
                    continue; 

                var splitted = line.Split(";", 2, StringSplitOptions.RemoveEmptyEntries);

                if (splitted.Length != 2)
                {
                    continue;
                }

                var clientName = splitted[0].Trim(' ', '\t');
                var ja3 = splitted[1].Trim(' ', '\t'); ;

                var fingerPrint = Ja3FingerPrint.Parse(ja3);
                var normalizedFingerPrint = Ja3FingerPrint.Parse(fingerPrint.ToString(true));

                yield return (clientName, normalizedFingerPrint);
            }
        }

        public static IEnumerable<object[]> LoadTestDataAsObject()
        {
            var testDatas = LoadTestData();
            var testedHosts = new List<string> {
                "https://check.ja3.zone/", "https://docs.fluxzy.io/nothing"
                , "https://www.google.com/nothing",
                 "https://extranet.2befficient.fr/nothing"
            };

            foreach (var testData in testDatas)
            {
                foreach (var host in testedHosts)
                {
                    yield return new object[] { host, testData.FriendlyName, testData.FingerPrint.ToString() };
                }
            }
        }
    }
}