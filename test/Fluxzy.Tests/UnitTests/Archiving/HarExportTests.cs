// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Archiving.Har;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Archiving
{
    public class HarExportTests : ProduceDeletableItem
    {
        private static readonly DateTime T0 = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        private static readonly HttpArchiveSavingSetting AlwaysSave = new() {
            Policy = HttpArchiveSavingBodyPolicy.AlwaysSave
        };

        [Fact]
        public async Task Log_Has_Version_Creator_And_One_Entry_Per_Exchange()
        {
            var dump = NewDump();
            dump.AddConnection(1);
            dump.AddExchange(1, 1, "GET", "https://example.com/a");
            dump.AddExchange(2, 1, "GET", "https://example.com/b");

            var log = (await Export(dump)).GetProperty("log");

            Assert.Equal("1.2", log.GetProperty("version").GetString());
            Assert.Equal("fluxzy", log.GetProperty("creator").GetProperty("name").GetString());
            Assert.False(string.IsNullOrEmpty(log.GetProperty("creator").GetProperty("version").GetString()));
            Assert.Equal(2, log.GetProperty("entries").GetArrayLength());
        }

        [Fact]
        public async Task Entry_Request_Main_Properties()
        {
            var dump = NewDump();
            dump.AddConnection(1);

            dump.AddExchange(1, 1, "GET", "https://example.com/path?a=1&b=2",
                requestHeaders: new[] {
                    ("User-Agent", "unit-test"),
                    ("Cookie", "first=1; second=2")
                });

            var entry = Entry(await Export(dump), 1);
            var request = entry.GetProperty("request");

            Assert.Equal("GET", request.GetProperty("method").GetString());
            Assert.Equal("https://example.com/path?a=1&b=2", request.GetProperty("url").GetString());
            Assert.Equal("HTTP/1.1", request.GetProperty("httpVersion").GetString());

            Assert.Contains(request.GetProperty("headers").EnumerateArray(),
                h => h.GetProperty("name").GetString() == "User-Agent"
                     && h.GetProperty("value").GetString() == "unit-test");

            var queryString = request.GetProperty("queryString").EnumerateArray()
                                     .ToDictionary(q => q.GetProperty("name").GetString()!,
                                         q => q.GetProperty("value").GetString());

            Assert.Equal(2, queryString.Count);
            Assert.Equal("1", queryString["a"]);
            Assert.Equal("2", queryString["b"]);

            var cookies = request.GetProperty("cookies").EnumerateArray()
                                 .ToDictionary(q => q.GetProperty("name").GetString()!,
                                     q => q.GetProperty("value").GetString());

            Assert.Equal(2, cookies.Count);
            Assert.Equal("1", cookies["first"]);
            Assert.Equal("2", cookies["second"]);

            Assert.Equal(T0, entry.GetProperty("startedDateTime").GetDateTime().ToUniversalTime());
            Assert.Equal(60, entry.GetProperty("time").GetInt32());
        }

        [Fact]
        public async Task Entry_Response_Main_Properties()
        {
            var dump = NewDump();
            dump.AddConnection(1);

            dump.AddExchange(1, 1, "GET", "https://example.com/",
                responseHeaders: new[] {
                    ("Content-Type", "text/plain; charset=utf-8"),
                    ("Set-Cookie", "sid=abc; Path=/; HttpOnly; Secure")
                },
                responseBody: Encoding.UTF8.GetBytes("hello world"));

            var response = Entry(await Export(dump), 1).GetProperty("response");

            Assert.Equal(200, response.GetProperty("status").GetInt32());
            Assert.Equal("HTTP/1.1", response.GetProperty("httpVersion").GetString());
            Assert.Equal(11, response.GetProperty("bodySize").GetInt64());

            var content = response.GetProperty("content");

            Assert.Equal("hello world", content.GetProperty("text").GetString());
            Assert.Equal(11, content.GetProperty("size").GetInt64());
            Assert.Equal("text/plain; charset=utf-8", content.GetProperty("mimeType").GetString());
            Assert.False(content.TryGetProperty("encoding", out _));

            var cookie = Assert.Single(response.GetProperty("cookies").EnumerateArray());

            Assert.Equal("sid", cookie.GetProperty("name").GetString());
            Assert.Equal("abc", cookie.GetProperty("value").GetString());
            Assert.Equal("/", cookie.GetProperty("path").GetString());
            Assert.True(cookie.GetProperty("httpOnly").GetBoolean());
            Assert.True(cookie.GetProperty("secure").GetBoolean());
        }

        [Fact]
        public async Task Entry_Uses_Har_Spec_Casing_For_ServerIPAddress()
        {
            var dump = NewDump();
            dump.AddConnection(1, remoteAddress: "93.184.216.34");
            dump.AddExchange(1, 1, "GET", "https://example.com/");

            var entry = Entry(await Export(dump), 1);

            Assert.True(entry.TryGetProperty("serverIPAddress", out var address),
                "entry has no 'serverIPAddress' property");

            Assert.Equal("93.184.216.34", address.GetString());
            Assert.Equal("1", entry.GetProperty("connection").GetString());
        }

        [Fact]
        public async Task PostData_Form_Url_Encoded_Has_Params()
        {
            var dump = NewDump();
            dump.AddConnection(1);

            dump.AddExchange(1, 1, "POST", "https://example.com/form",
                requestHeaders: new[] { ("Content-Type", "application/x-www-form-urlencoded") },
                requestBody: Encoding.UTF8.GetBytes("name=john&mail=a%40b.com"));

            var postData = Entry(await Export(dump), 1).GetProperty("request").GetProperty("postData");

            var @params = postData.GetProperty("params").EnumerateArray()
                                  .ToDictionary(q => q.GetProperty("name").GetString()!,
                                      q => q.GetProperty("value").GetString());

            Assert.Equal("name=john&mail=a%40b.com", postData.GetProperty("text").GetString());
            Assert.Equal(2, @params.Count);
            Assert.Equal("john", @params["name"]);
            Assert.Equal("a@b.com", @params["mail"]);
        }

        [Fact]
        public async Task Binary_Request_Body_Is_Base64()
        {
            var payload = BinaryPayload(4096);

            var dump = NewDump();
            dump.AddConnection(1);

            dump.AddExchange(1, 1, "POST", "https://example.com/bin",
                requestHeaders: new[] { ("Content-Type", "application/octet-stream") },
                requestBody: payload);

            var request = Entry(await Export(dump), 1).GetProperty("request");
            var postData = request.GetProperty("postData");

            Assert.Equal(payload.Length, request.GetProperty("bodySize").GetInt64());
            Assert.Equal("base64", postData.GetProperty("comment").GetString());
            Assert.Equal(payload, Convert.FromBase64String(postData.GetProperty("text").GetString()!));
        }

        [Fact]
        public async Task Binary_Response_Body_Is_Base64()
        {
            var payload = BinaryPayload(4096);

            var dump = NewDump();
            dump.AddConnection(1);

            dump.AddExchange(1, 1, "GET", "https://example.com/img",
                responseHeaders: new[] { ("Content-Type", "image/png") },
                responseBody: payload);

            var content = Entry(await Export(dump), 1).GetProperty("response").GetProperty("content");

            Assert.Equal("base64", content.GetProperty("encoding").GetString());
            Assert.Equal(payload, Convert.FromBase64String(content.GetProperty("text").GetString()!));
            Assert.Equal(payload.Length, content.GetProperty("size").GetInt64());
        }

        [Fact]
        public async Task Exchange_Without_Response_Is_Exported()
        {
            var dump = NewDump();
            dump.AddConnection(1);
            dump.AddExchange(1, 1, "GET", "https://example.com/pending", noResponse: true);

            var entry = Entry(await Export(dump), 1);

            Assert.Equal(0, entry.GetProperty("response").GetProperty("status").GetInt32());
            Assert.Equal("GET", entry.GetProperty("request").GetProperty("method").GetString());
        }

        [Fact]
        public async Task Exchange_Without_Known_Connection_Is_Exported()
        {
            var dump = NewDump();
            dump.AddExchange(1, 0, "GET", "https://example.com/");

            var entry = Entry(await Export(dump), 1);

            Assert.False(entry.TryGetProperty("connection", out _));
            Assert.Equal(200, entry.GetProperty("response").GetProperty("status").GetInt32());
        }

        [Fact]
        public async Task Exchange_Ids_Filter_Is_Applied()
        {
            var dump = NewDump();
            dump.AddConnection(1);
            dump.AddExchange(1, 1, "GET", "https://example.com/1");
            dump.AddExchange(2, 1, "GET", "https://example.com/2");
            dump.AddExchange(3, 1, "GET", "https://example.com/3");

            var root = await Export(dump, exchangeIds: new HashSet<int> { 1, 3 });

            var ids = root.GetProperty("log").GetProperty("entries").EnumerateArray()
                          .Select(e => e.GetProperty("_exchangeId").GetInt32()).OrderBy(i => i).ToList();

            Assert.Equal(new[] { 1, 3 }, ids);
        }

        [Fact]
        public async Task Saving_Policy_Skip_Body()
        {
            var dump = NewDump();
            dump.AddConnection(1);

            dump.AddExchange(1, 1, "POST", "https://example.com/",
                requestHeaders: new[] { ("Content-Type", "text/plain") },
                responseHeaders: new[] { ("Content-Type", "text/plain") },
                requestBody: Encoding.UTF8.GetBytes("request"),
                responseBody: Encoding.UTF8.GetBytes("response"));

            var entry = Entry(await Export(dump, new HttpArchiveSavingSetting {
                Policy = HttpArchiveSavingBodyPolicy.SkipBody
            }), 1);

            var content = entry.GetProperty("response").GetProperty("content");

            Assert.False(content.TryGetProperty("text", out _));
            Assert.Equal(8, content.GetProperty("size").GetInt64());

            if (entry.GetProperty("request").TryGetProperty("postData", out var postData)
                && postData.TryGetProperty("text", out var text)) {
                Assert.Equal(string.Empty, text.GetString());
            }
        }

        [Theory]
        [InlineData(10, true)]
        [InlineData(11, false)]
        public async Task Saving_Policy_Max_Length(int bodyLength, bool expectSaved)
        {
            var body = new string('a', bodyLength);

            var dump = NewDump();
            dump.AddConnection(1);

            dump.AddExchange(1, 1, "POST", "https://example.com/",
                requestHeaders: new[] { ("Content-Type", "text/plain") },
                responseHeaders: new[] { ("Content-Type", "text/plain") },
                requestBody: Encoding.UTF8.GetBytes(body),
                responseBody: Encoding.UTF8.GetBytes(body));

            var entry = Entry(await Export(dump, new HttpArchiveSavingSetting {
                Policy = HttpArchiveSavingBodyPolicy.MaxLengthSave,
                HarLimitMaxBodyLength = 10
            }), 1);

            var content = entry.GetProperty("response").GetProperty("content");
            var postDataText = entry.GetProperty("request").GetProperty("postData").GetProperty("text").GetString();

            if (expectSaved) {
                Assert.Equal(body, content.GetProperty("text").GetString());
                Assert.Equal(body, postDataText);
            }
            else {
                Assert.False(content.TryGetProperty("text", out _));
                Assert.Equal(string.Empty, postDataText);
            }
        }

        [Fact]
        public async Task Timings_Of_First_Exchange_On_Connection()
        {
            var dump = NewDump();
            dump.AddConnection(1);
            dump.AddExchange(1, 1, "GET", "https://example.com/");

            var timings = Entry(await Export(dump), 1).GetProperty("timings");

            Assert.Equal(1, timings.GetProperty("blocked").GetDouble());
            Assert.Equal(2, timings.GetProperty("dns").GetDouble());
            Assert.Equal(3, timings.GetProperty("connect").GetDouble());
            Assert.Equal(4, timings.GetProperty("ssl").GetDouble());
            Assert.Equal(2, timings.GetProperty("send").GetDouble());
            Assert.Equal(38, timings.GetProperty("wait").GetDouble());
            Assert.Equal(9, timings.GetProperty("receive").GetDouble());
        }

        [Fact]
        public void Packager_Export_As_Http_Archive_Produces_Valid_Json_File()
        {
            var dump = NewDump();
            dump.AddConnection(1);
            dump.AddExchange(1, 1, "GET", "https://example.com/");

            var outFile = GetRegisteredRandomFile();

            Packager.ExportAsHttpArchive(dump.Directory, outFile);

            using var document = JsonDocument.Parse(File.ReadAllBytes(outFile));

            Assert.Equal(1, document.RootElement.GetProperty("log").GetProperty("entries").GetArrayLength());
        }

        [Theory]
        [InlineData("capture.har", true)]
        [InlineData("capture.HAR", true)]
        [InlineData("capture.fxzy", false)]
        [InlineData("har", false)]
        public void Should_Apply_To(string fileName, bool expected)
        {
            Assert.Equal(expected, new HttpArchivePackager().ShouldApplyTo(fileName));
        }

        private DumpBuilder NewDump()
        {
            return new DumpBuilder(GetRegisteredRandomDirectory());
        }

        private static async Task<JsonElement> Export(
            DumpBuilder dump, HttpArchiveSavingSetting? setting = null, HashSet<int>? exchangeIds = null)
        {
            var packager = new HttpArchivePackager(setting ?? AlwaysSave);

            using var memoryStream = new MemoryStream();

            await packager.Pack(dump.Directory, memoryStream, exchangeIds);

            using var document = JsonDocument.Parse(memoryStream.ToArray());

            return document.RootElement.Clone();
        }

        private static JsonElement Entry(JsonElement root, int exchangeId)
        {
            return root.GetProperty("log").GetProperty("entries").EnumerateArray()
                       .Single(e => e.GetProperty("_exchangeId").GetInt32() == exchangeId);
        }

        private static byte[] BinaryPayload(int length)
        {
            var payload = new byte[length];
            new Random(9).NextBytes(payload);

            payload[0] = 0;
            payload[1] = 1;

            return payload;
        }

        private static byte[] Gzip(byte[] raw)
        {
            using var output = new MemoryStream();

            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true)) {
                gzip.Write(raw, 0, raw.Length);
            }

            return output.ToArray();
        }

        private static void ShiftMetrics(ExchangeMetrics metrics, TimeSpan offset)
        {
            metrics.ReceivedFromProxy += offset;
            metrics.RetrievingPool += offset;
            metrics.RequestHeaderSending += offset;
            metrics.RequestHeaderSent += offset;
            metrics.RequestBodySent += offset;
            metrics.ResponseHeaderStart += offset;
            metrics.ResponseHeaderEnd += offset;
            metrics.ResponseBodyStart += offset;
            metrics.ResponseBodyEnd += offset;
        }

        private sealed class DumpBuilder
        {
            private readonly DirectoryArchiveWriter _writer;

            public DumpBuilder(string directory)
            {
                Directory = directory;
                _writer = new DirectoryArchiveWriter(directory, null);
                _writer.Init();
            }

            public string Directory { get; }

            public void AddConnection(int id, bool secure = true, string remoteAddress = "10.0.0.1")
            {
                var connection = new ConnectionInfo(
                    id, new AuthorityInfo("example.com", secure ? 443 : 80, secure), null, 1,
                    T0.AddMilliseconds(1), T0.AddMilliseconds(3),
                    T0.AddMilliseconds(3), T0.AddMilliseconds(6),
                    secure ? T0.AddMilliseconds(6) : default, secure ? T0.AddMilliseconds(10) : default,
                    50000, "127.0.0.1", remoteAddress, "HTTP/1.1");

                _writer.Update(connection, CancellationToken.None);
            }

            public void AddExchange(
                int id, int connectionId, string method, string url,
                (string Name, string Value)[]? requestHeaders = null,
                (string Name, string Value)[]? responseHeaders = null,
                byte[]? requestBody = null,
                byte[]? responseBody = null,
                int statusCode = 200,
                bool noResponse = false,
                string? comment = null,
                Action<ExchangeMetrics>? configureMetrics = null)
            {
                var uri = new Uri(url);

                var metrics = new ExchangeMetrics {
                    ReceivedFromProxy = T0,
                    RetrievingPool = T0.AddMilliseconds(1),
                    RequestHeaderSending = T0.AddMilliseconds(10),
                    RequestHeaderSent = T0.AddMilliseconds(11),
                    RequestBodySent = T0.AddMilliseconds(12)
                };

                if (!noResponse) {
                    metrics.ResponseHeaderStart = T0.AddMilliseconds(50);
                    metrics.ResponseHeaderEnd = T0.AddMilliseconds(51);
                    metrics.ResponseBodyStart = T0.AddMilliseconds(52);
                    metrics.ResponseBodyEnd = T0.AddMilliseconds(60);
                }

                configureMetrics?.Invoke(metrics);

                var exchange = new ExchangeInfo(
                    id, connectionId, "HTTP/1.1",
                    new RequestHeaderInfo(method, url,
                        (requestHeaders ?? Array.Empty<(string, string)>())
                        .Select(h => new HeaderFieldInfo(h.Item1, h.Item2))),
                    noResponse
                        ? null
                        : new ResponseHeaderInfo(statusCode,
                            (responseHeaders ?? Array.Empty<(string, string)>())
                            .Select(h => new HeaderFieldInfo(h.Item1, h.Item2)).ToList()),
                    metrics, "10.0.0.1", false, comment, null, false, new(), null, new(),
                    uri.Host, uri.Port, uri.Scheme == "https");

                _writer.Update(exchange, CancellationToken.None);

                if (requestBody != null) {
                    using var stream = _writer.CreateRequestBodyStream(id);
                    stream.Write(requestBody, 0, requestBody.Length);
                }

                if (responseBody != null) {
                    using var stream = _writer.CreateResponseBodyStream(id);
                    stream.Write(responseBody, 0, responseBody.Length);
                }
            }
        }

        private sealed class SlowStream : MemoryStream
        {
            private readonly int _maxPerRead;

            public SlowStream(byte[] data, int maxPerRead)
                : base(data)
            {
                _maxPerRead = maxPerRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, _maxPerRead));
            }

            public override int Read(Span<byte> buffer)
            {
                return base.Read(buffer.Slice(0, Math.Min(buffer.Length, _maxPerRead)));
            }
        }
    }
}
