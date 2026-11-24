// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    /// <summary>
    ///     Custom properties of the archive meta information, and what a writer must keep
    ///     when it works on a directory that already holds a capture.
    /// </summary>
    public class ArchiveMetaPropertyTests : IDisposable
    {
        private const string MutesKey = "fluxzy.security.mutes";

        private const string MutesValue =
            "{\"version\":1,\"mutes\":[{\"host\":\"example.com\",\"checkId\":\"cookie-no-secure\",\"key\":\"séssion\"}]}";

        private readonly string _baseDirectory;
        private readonly string _rootDirectory;

        public ArchiveMetaPropertyTests()
        {
            _rootDirectory = Path.Combine(Path.GetTempPath(),
                "fluxzy-metaproperty-tests", Guid.NewGuid().ToString("N"));

            _baseDirectory = Path.Combine(_rootDirectory, "capture");

            Directory.CreateDirectory(_baseDirectory);
        }

        private string MetaPath => Path.Combine(_baseDirectory, "meta.json");

        public void Dispose()
        {
            try {
                if (Directory.Exists(_rootDirectory))
                    Directory.Delete(_rootDirectory, true);
            }
            catch {
                // best effort cleanup
            }
        }

        private static FluxzySetting BuildSetting(int connectionPerHost)
        {
            var setting = FluxzySetting.CreateDefault();
            setting.SetConnectionPerHost(connectionPerHost);

            return setting;
        }

        private DirectoryArchiveWriter CreateInitializedWriter(int connectionPerHost = 7)
        {
            var writer = new DirectoryArchiveWriter(_baseDirectory, null, BuildSetting(connectionPerHost));
            writer.Init();

            return writer;
        }

        private ArchiveMetaInformation ReadDirectoryMeta(string? directory = null)
        {
            using var reader = new DirectoryArchiveReader(directory ?? _baseDirectory);

            return reader.ReadMetaInformation();
        }

        private async Task<string> Pack(string? directory = null)
        {
            var fxzyPath = Path.Combine(_rootDirectory, Guid.NewGuid().ToString("N") + ".fxzy");

            await new FxzyDirectoryPackager().Pack(directory ?? _baseDirectory, fxzyPath);

            return fxzyPath;
        }

        private static ArchiveMetaInformation ReadPackedMeta(string fxzyPath)
        {
            using var reader = new FluxzyArchiveReader(fxzyPath);

            return reader.ReadMetaInformation();
        }

        private void EditRawMeta(Action<JsonObject> edit)
        {
            var meta = JsonNode.Parse(File.ReadAllText(MetaPath))!.AsObject();

            edit(meta);

            File.WriteAllText(MetaPath, meta.ToJsonString());
        }

        [Fact]
        public void Property_Is_Readable_From_Directory()
        {
            var writer = CreateInitializedWriter();

            var modified = writer.UpdateProperty(MutesKey, MutesValue);

            writer.Dispose();

            var meta = ReadDirectoryMeta();

            Assert.True(modified);
            Assert.Equal(MutesValue, Assert.Single(meta.Properties, p => p.Key == MutesKey).Value);
        }

        [Fact]
        public async Task Property_Survives_Pack_And_Reopen()
        {
            var writer = CreateInitializedWriter();

            writer.UpdateProperty(MutesKey, MutesValue);
            writer.Dispose();

            var fxzyPath = await Pack();

            Assert.Equal(MutesValue, ReadPackedMeta(fxzyPath).Properties[MutesKey]);

            // Reopen as an application does: unpack to a new directory, start a proxy writer on it
            var reopenedDirectory = Path.Combine(_rootDirectory, "reopened");

            new FxzyImportEngine(new FxzyDirectoryPackager()).WriteToDirectory(fxzyPath, reopenedDirectory);

            Assert.Equal(MutesValue, ReadDirectoryMeta(reopenedDirectory).Properties[MutesKey]);

            var reopenedWriter = new DirectoryArchiveWriter(reopenedDirectory, null, BuildSetting(3));

            reopenedWriter.Init();
            reopenedWriter.SetResolvedEndPoints(new[] { new IPEndPoint(IPAddress.Loopback, 2222) });
            reopenedWriter.Dispose();

            Assert.Equal(MutesValue, ReadDirectoryMeta(reopenedDirectory).Properties[MutesKey]);

            // And save again
            var secondFxzyPath = await Pack(reopenedDirectory);

            Assert.Equal(MutesValue, ReadPackedMeta(secondFxzyPath).Properties[MutesKey]);
        }

        [Fact]
        public async Task Property_Is_Removed_With_Null_Value()
        {
            var writer = CreateInitializedWriter();

            writer.UpdateProperty(MutesKey, MutesValue);
            writer.UpdateProperty("other", "kept");

            var removed = writer.UpdateProperty(MutesKey, null);
            var removedAgain = writer.UpdateProperty(MutesKey, null);

            writer.Dispose();

            Assert.True(removed);
            Assert.False(removedAgain);

            var directoryMeta = ReadDirectoryMeta();

            Assert.False(directoryMeta.Properties.ContainsKey(MutesKey));
            Assert.Equal("kept", directoryMeta.Properties["other"]);

            var packedMeta = ReadPackedMeta(await Pack());

            Assert.False(packedMeta.Properties.ContainsKey(MutesKey));
            Assert.Equal("kept", packedMeta.Properties["other"]);
        }

        [Fact]
        public void Removing_A_Property_From_An_Archive_Without_Any_Changes_Nothing()
        {
            var writer = CreateInitializedWriter();
            var before = File.ReadAllText(MetaPath);

            var removed = writer.UpdateProperty(MutesKey, null);

            writer.Dispose();

            Assert.False(removed);
            Assert.Equal(before, File.ReadAllText(MetaPath));
        }

        [Fact]
        public void Property_Value_Is_Replaced()
        {
            var writer = CreateInitializedWriter();

            writer.UpdateProperty(MutesKey, "first");

            var modified = writer.UpdateProperty(MutesKey, "second");

            writer.Dispose();

            Assert.True(modified);
            Assert.Equal("second", ReadDirectoryMeta().Properties[MutesKey]);
        }

        [Fact]
        public void Unchanged_Property_Does_Not_Rewrite_Meta()
        {
            var writer = CreateInitializedWriter();

            writer.UpdateProperty(MutesKey, MutesValue);

            // The writer produces an indented file: a compact one shows whether it was rewritten
            EditRawMeta(_ => { });

            var compact = File.ReadAllText(MetaPath);

            var modified = writer.UpdateProperty(MutesKey, MutesValue);

            writer.Dispose();

            Assert.False(modified);
            Assert.Equal(compact, File.ReadAllText(MetaPath));
        }

        [Theory]
        [InlineData("MyKey", "value")]
        [InlineData("my key", "value with \"quotes\" and \\ backslash")]
        [InlineData("a.b.c", "line1\nline2\ttab")]
        [InlineData("clé-ключ-鍵", "välue-значение-値 <>&+'")]
        [InlineData("key", "")]
        public void Keys_And_Values_Are_Stored_As_They_Are(string key, string value)
        {
            var writer = CreateInitializedWriter();

            writer.UpdateProperty(key, value);
            writer.Dispose();

            var meta = ReadDirectoryMeta();

            Assert.Equal(value, Assert.Single(meta.Properties).Value);
            Assert.Equal(key, Assert.Single(meta.Properties).Key);
        }

        [Fact]
        public void Keys_Are_Case_Sensitive()
        {
            var writer = CreateInitializedWriter();

            writer.UpdateProperty("Key", "upper");
            writer.UpdateProperty("key", "lower");
            writer.UpdateProperty("Key", null);
            writer.Dispose();

            var meta = ReadDirectoryMeta();

            Assert.Equal("lower", Assert.Single(meta.Properties).Value);
            Assert.Equal("key", Assert.Single(meta.Properties).Key);
        }

        [Fact]
        public void Null_Or_Empty_Key_Is_Rejected()
        {
            var writer = CreateInitializedWriter();

            Assert.Throws<ArgumentNullException>(() => writer.UpdateProperty(null!, "value"));
            Assert.Throws<ArgumentException>(() => writer.UpdateProperty(string.Empty, "value"));

            writer.Dispose();

            Assert.Empty(ReadDirectoryMeta().Properties);
        }

        [Fact]
        public void Writer_Without_Meta_Ignores_Properties()
        {
            RealtimeArchiveWriter writer = new EventOnlyArchiveWriter();

            Assert.False(writer.UpdateProperty(MutesKey, MutesValue));
            Assert.False(writer.UpdateProperty(MutesKey, null));
        }

        /// <summary>
        ///     What the proxy does each time it starts on a directory: a new writer, Init, then the
        ///     resolved endpoints. It used to replace meta.json with a blank one.
        /// </summary>
        [Fact]
        public void New_Session_On_Existing_Capture_Keeps_Archive_Content_And_Replaces_Session()
        {
            var tag = new Tag(Guid.NewGuid(), "keep-me");
            var firstWriter = CreateInitializedWriter(7);

            firstWriter.UpdateTags(new[] { tag });
            firstWriter.UpdateProperty(MutesKey, MutesValue);
            firstWriter.SetResolvedEndPoints(new[] { new IPEndPoint(IPAddress.Loopback, 1111) });
            firstWriter.Dispose();

            // A capture made earlier, elsewhere, with a saved view filter
            EditRawMeta(meta => {
                meta["captureDate"] = "2024-01-25T23:45:01+01:00";
                meta["fluxzyVersion"] = "0.0.1.0";
                meta["archiveVersion"] = "0.2.0";
                meta["environmentInformation"]!["machineName"] = "another-machine";

                meta["viewFilters"] = JsonSerializer.SerializeToNode(
                    new List<Filter> { new HostFilter("example.com") },
                    GlobalArchiveOption.DefaultSerializerOptions);
            });

            var secondWriter = CreateInitializedWriter(3);

            secondWriter.SetResolvedEndPoints(new[] { new IPEndPoint(IPAddress.Loopback, 2222) });
            secondWriter.Dispose();

            var meta = ReadDirectoryMeta();

            // Archive content is kept
            Assert.Equal(tag, Assert.Single(meta.Tags));
            Assert.Equal(MutesValue, meta.Properties[MutesKey]);
            Assert.Equal(new DateTime(2024, 1, 25, 22, 45, 1, DateTimeKind.Utc), meta.CaptureDate.ToUniversalTime());
            Assert.Equal("example.com", Assert.IsType<HostFilter>(Assert.Single(meta.ViewFilters)).Pattern);

            // The session is the one of the last writer
            Assert.Equal(3, meta.CapturedSetting!.ConnectionPerHost);
            Assert.Equal(2222, Assert.Single(meta.ResolvedEndPoints).Port);
            Assert.NotEqual("another-machine", meta.EnvironmentInformation!.MachineName);
            Assert.Equal(new ArchiveMetaInformation().FluxzyVersion, meta.FluxzyVersion);
            Assert.Equal(new ArchiveMetaInformation().ArchiveVersion, meta.ArchiveVersion);
        }

        /// <summary>
        ///     The same with a running proxy, as an application that reloads its proxy on the working
        ///     directory of the opened capture after a setting or a rule changed.
        /// </summary>
        [Fact]
        public async Task Proxy_Restarted_On_The_Same_Directory_Keeps_Tags_And_Properties()
        {
            await using var server = CountingHttpServer.Start();

            var tag = new Tag(Guid.NewGuid(), "from-rule");

            FluxzySetting CreateProxySetting(int connectionPerHost)
            {
                var setting = FluxzySetting.CreateLocalRandomPort();

                setting.SetOutDirectory(_baseDirectory);
                setting.SetConnectionPerHost(connectionPerHost);
                setting.AddAlterationRules(new ApplyTagAction { Tag = tag }, AnyFilter.Default);

                return setting;
            }

            await using (var proxy = new Proxy(CreateProxySetting(7))) {
                var endPoint = proxy.Run().First();

                using var client = new HttpClient(new HttpClientHandler {
                    Proxy = new WebProxy($"http://{endPoint}"),
                    UseProxy = true
                });

                using var response = await client.GetAsync($"http://127.0.0.1:{server.Port}/");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(proxy.Writer.UpdateProperty(MutesKey, MutesValue));
            }

            var firstMeta = ReadDirectoryMeta();

            Assert.Equal(tag, Assert.Single(firstMeta.Tags));
            Assert.Equal(MutesValue, firstMeta.Properties[MutesKey]);
            Assert.Equal(7, firstMeta.CapturedSetting!.ConnectionPerHost);

            int secondPort;

            await using (var proxy = new Proxy(CreateProxySetting(3))) {
                secondPort = proxy.Run().First().Port;

                // Nothing changed for the writer of this second session
                Assert.False(proxy.Writer.UpdateProperty(MutesKey, MutesValue));
            }

            var secondMeta = ReadDirectoryMeta();

            Assert.Equal(tag, Assert.Single(secondMeta.Tags));
            Assert.Equal(MutesValue, secondMeta.Properties[MutesKey]);
            Assert.Equal(firstMeta.CaptureDate, secondMeta.CaptureDate);
            Assert.Equal(3, secondMeta.CapturedSetting!.ConnectionPerHost);
            Assert.Equal(secondPort, Assert.Single(secondMeta.ResolvedEndPoints).Port);

            using var reader = new DirectoryArchiveReader(_baseDirectory);

            Assert.Equal(tag, Assert.Single(Assert.Single(reader.ReadAllExchanges()).Tags));
        }

        [Fact]
        public void Writer_Without_Captured_Setting_Keeps_Session()
        {
            var firstWriter = CreateInitializedWriter(7);

            firstWriter.UpdateProperty(MutesKey, MutesValue);
            firstWriter.Dispose();

            EditRawMeta(meta => {
                meta["fluxzyVersion"] = "0.0.1.0";
                meta["environmentInformation"]!["machineName"] = "another-machine";
            });

            var secondWriter = new DirectoryArchiveWriter(_baseDirectory, null);

            secondWriter.Init();
            secondWriter.UpdateTags(new[] { new Tag(Guid.NewGuid(), "added") });
            secondWriter.UpdateProperty("other", "value");
            secondWriter.Dispose();

            var meta = ReadDirectoryMeta();

            Assert.Equal(7, meta.CapturedSetting!.ConnectionPerHost);
            Assert.Equal("another-machine", meta.EnvironmentInformation!.MachineName);
            Assert.Equal("0.0.1.0", meta.FluxzyVersion);
            Assert.Equal("added", Assert.Single(meta.Tags).Value);
            Assert.Equal(MutesValue, meta.Properties[MutesKey]);
            Assert.Equal("value", meta.Properties["other"]);
        }

        /// <summary>
        ///     Pasting or duplicating exchanges uses a short-lived writer that is never initialized.
        /// </summary>
        [Fact]
        public void Writer_Without_Init_Keeps_Meta_When_An_Exchange_Brings_A_Tag()
        {
            var existingTag = new Tag(Guid.NewGuid(), "existing");
            var firstWriter = CreateInitializedWriter(7);

            firstWriter.UpdateTags(new[] { existingTag });
            firstWriter.UpdateProperty(MutesKey, MutesValue);
            firstWriter.Dispose();

            ExchangeInfo exchange;

            using (var source = new FluxzyArchiveReader("_Files/Archives/with-request-payload.fxzy")) {
                exchange = source.ReadAllExchanges().First();
            }

            var pastedTag = new Tag(Guid.NewGuid(), "pasted");

            exchange.Tags.Add(pastedTag);

            var pasteWriter = new DirectoryArchiveWriter(_baseDirectory, null);

            pasteWriter.Update(exchange, CancellationToken.None);
            pasteWriter.Update(exchange, CancellationToken.None);
            pasteWriter.Dispose();

            var meta = ReadDirectoryMeta();

            Assert.Equal(new[] { existingTag, pastedTag }.OrderBy(t => t.Value), meta.Tags.OrderBy(t => t.Value));
            Assert.Equal(MutesValue, meta.Properties[MutesKey]);
            Assert.Equal(7, meta.CapturedSetting!.ConnectionPerHost);
        }

        [Fact]
        public void Tags_Stored_By_Another_Writer_Are_Kept()
        {
            var firstTag = new Tag(Guid.NewGuid(), "first");
            var secondTag = new Tag(Guid.NewGuid(), "second");

            var firstWriter = CreateInitializedWriter();
            var secondWriter = CreateInitializedWriter();

            firstWriter.UpdateTags(new[] { firstTag });
            secondWriter.UpdateTags(new[] { secondTag });
            firstWriter.UpdateTags(new[] { firstTag, secondTag });

            firstWriter.Dispose();
            secondWriter.Dispose();

            Assert.Equal(new[] { firstTag, secondTag }.OrderBy(t => t.Value),
                ReadDirectoryMeta().Tags.OrderBy(t => t.Value));
        }

        /// <summary>
        ///     Content the writer does not know (written by a newer version, or a captured setting
        ///     holding a filter or an action from a plugin) must go through an edit untouched.
        /// </summary>
        [Fact]
        public void Content_Unknown_To_The_Writer_Is_Kept()
        {
            CreateInitializedWriter().Dispose();

            EditRawMeta(meta => {
                meta["addedByANewerVersion"] = new JsonObject { ["answer"] = 42 };

                meta["capturedSetting"] = new JsonObject {
                    ["alterationRules"] = new JsonArray(new JsonObject {
                        ["filter"] = new JsonObject { ["typeKind"] = "FilterFromAPlugin" },
                        ["action"] = new JsonObject { ["typeKind"] = "ActionFromAPlugin" }
                    })
                };
            });

            var writer = new DirectoryArchiveWriter(_baseDirectory, null);

            writer.Init();
            writer.UpdateProperty(MutesKey, MutesValue);
            writer.UpdateTags(new[] { new Tag(Guid.NewGuid(), "added") });
            writer.Dispose();

            var rawMeta = JsonNode.Parse(File.ReadAllText(MetaPath))!.AsObject();

            Assert.Equal(42, rawMeta["addedByANewerVersion"]!["answer"]!.GetValue<int>());

            Assert.Equal("FilterFromAPlugin",
                rawMeta["capturedSetting"]!["alterationRules"]![0]!["filter"]!["typeKind"]!.GetValue<string>());

            Assert.Equal(MutesValue, rawMeta["properties"]![MutesKey]!.GetValue<string>());
            Assert.Equal("added", rawMeta["tags"]![0]!["value"]!.GetValue<string>());
        }

        [Theory]
        [InlineData("")]
        [InlineData("{ \"captureDate\": \"2024-01-25T23:45:01+01:00\", \"tags\": [")]
        [InlineData("null")]
        [InlineData("[]")]
        public void Unreadable_Meta_Is_Replaced(string content)
        {
            File.WriteAllText(MetaPath, content);

            var writer = CreateInitializedWriter(7);

            var modified = writer.UpdateProperty(MutesKey, MutesValue);

            writer.Dispose();

            var meta = ReadDirectoryMeta();

            Assert.True(modified);
            Assert.Equal(MutesValue, meta.Properties[MutesKey]);
            Assert.Equal(7, meta.CapturedSetting!.ConnectionPerHost);
        }

        [Fact]
        public void Properties_That_Are_Not_An_Object_Are_Replaced()
        {
            CreateInitializedWriter().Dispose();

            EditRawMeta(meta => meta["properties"] = null);

            var writer = new DirectoryArchiveWriter(_baseDirectory, null);

            var modified = writer.UpdateProperty(MutesKey, MutesValue);

            writer.Dispose();

            Assert.True(modified);
            Assert.Equal(MutesValue, ReadDirectoryMeta().Properties[MutesKey]);
        }

        [Fact]
        public void Meta_Is_Written_Without_Leaving_A_Temporary_File()
        {
            var writer = CreateInitializedWriter();

            writer.UpdateProperty(MutesKey, MutesValue);
            writer.UpdateTags(new[] { new Tag(Guid.NewGuid(), "tag") });
            writer.Dispose();

            var files = Directory.GetFiles(_baseDirectory).Select(Path.GetFileName);

            Assert.Equal(new[] { "meta.json" }, files);
        }

        [Fact]
        public async Task Concurrent_Writers_On_The_Same_Directory_Keep_Every_Change()
        {
            const int writerCount = 4;
            const int changePerWriter = 25;

            CreateInitializedWriter().Dispose();

            using var readerStop = new CancellationTokenSource();

            // meta.json must be complete at any time for a reader
            var readerTask = Task.Run(async () => {
                var readCount = 0;

                while (!readerStop.IsCancellationRequested) {
                    try {
                        var content = await File.ReadAllBytesAsync(MetaPath);

                        Assert.NotNull(JsonSerializer.Deserialize<ArchiveMetaInformation>(content,
                            GlobalArchiveOption.DefaultSerializerOptions));

                        readCount++;
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                        // On Windows, the file may be briefly unavailable while it is replaced
                    }

                    await Task.Delay(1);
                }

                return readCount;
            });

            var writerTasks = Enumerable.Range(0, writerCount).Select(writerIndex => Task.Run(() => {
                // The last writer is not initialized, as the ones used to paste exchanges
                var writer = new DirectoryArchiveWriter(_baseDirectory, null,
                    writerIndex % 2 == 0 ? BuildSetting(writerIndex + 1) : null);

                if (writerIndex != writerCount - 1)
                    writer.Init();

                Parallel.For(0, changePerWriter, changeIndex => {
                    writer.UpdateProperty($"writer-{writerIndex}.{changeIndex}", $"{writerIndex}-{changeIndex}");
                    writer.UpdateTags(new[] { new Tag(Guid.NewGuid(), $"tag-{writerIndex}-{changeIndex}") });

                    if (changeIndex % 5 == 0)
                        writer.SetResolvedEndPoints(new[] { new IPEndPoint(IPAddress.Loopback, 1000 + changeIndex) });
                });

                writer.Dispose();
            })).ToArray();

            await Task.WhenAll(writerTasks);

            readerStop.Cancel();

            var readCount = await readerTask;
            var meta = ReadDirectoryMeta();

            Assert.True(readCount > 0);
            Assert.Equal(writerCount * changePerWriter, meta.Properties.Count);
            Assert.Equal(writerCount * changePerWriter, meta.Tags.Count);

            for (var writerIndex = 0; writerIndex < writerCount; writerIndex++) {
                for (var changeIndex = 0; changeIndex < changePerWriter; changeIndex++) {
                    Assert.Equal($"{writerIndex}-{changeIndex}",
                        meta.Properties[$"writer-{writerIndex}.{changeIndex}"]);
                }
            }
        }
    }
}
