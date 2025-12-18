// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Formatters
{
    public class ProducersActions : FormatterTestBase
    {
        [Theory]
        [InlineData(TestConstants.TestDomain, true, true)]
        [InlineData(TestConstants.TestDomain, false, true)]
        [InlineData("https://sandbox.fluxzy.io/swagger/index.html", false, true)]
        [InlineData("https://sandbox.fluxzy.io/swagger/index.html", true, true)]
        public async Task SaveResponseBodyAction(string url, bool decode, bool pass)
        {
            var randomFile = GetRegisteredRandomFile();
            var outFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile, setting => {
                setting.UseBouncyCastle = true;
            } );

            var archiveReaderProvider = new FromFileArchiveFileProvider(randomFile); 
            
            var producerFactory = new ProducerFactory(archiveReaderProvider, FormatSettings.Default);

            var action = new SaveResponseBodyAction(producerFactory); 
            
            var result = await action.Do(2, decode, outFile);

            if (pass) {
                Assert.True(result);
                Assert.True(System.IO.File.Exists(outFile));
            }
            else {
                Assert.False(result);
                Assert.False(System.IO.File.Exists(outFile));
            }
        }
        
        [Theory]
        [InlineData(TestConstants.TestDomain, true)]
        [InlineData(TestConstants.TestDomain, false)]
        public async Task SaveRequestBodyAction(string url, bool pass)
        {
            var randomFile = GetRegisteredRandomFile();
            var outFile = GetRegisteredRandomFile();
            var uri = new Uri(url);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            if (pass) {
                requestMessage.Content = new StringContent("Hello world");
            }

            await QuickArchiveBuilder.MakeQuickArchive(requestMessage, randomFile, setting => {
                setting.UseBouncyCastle = true;
            } );

            var archiveReaderProvider = new FromFileArchiveFileProvider(randomFile); 
            
            var producerFactory = new ProducerFactory(archiveReaderProvider, FormatSettings.Default);

            var action = new SaveRequestBodyProducerAction(producerFactory); 
            
            var result = await action.Do(2,  outFile);

            if (pass) {
                Assert.True(result);
                Assert.True(System.IO.File.Exists(outFile));
            }
            else {
                Assert.False(result);
                Assert.False(System.IO.File.Exists(outFile));
            }
        }
    }
}
