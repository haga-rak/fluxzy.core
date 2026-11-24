// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Fluxzy.Core;
using Fluxzy.Misc;
using Fluxzy.Rules.Filters;
using MessagePack;

namespace Fluxzy.Writers
{
    /// <summary>
    ///     Writes a capture into a dump directory.
    ///     <para>
    ///         Several writers can work on the same directory, at the same time or one after the other:
    ///         the proxy creates a new one each time it starts, and short-lived ones are created to
    ///         paste or import exchanges. For this reason <c>meta.json</c> on disk is the reference for
    ///         the archive meta information. Every change (tags, resolved endpoints, properties) reads
    ///         the file, applies the change and replaces the file, so what another writer stored is kept.
    ///     </para>
    /// </summary>
    public class DirectoryArchiveWriter : RealtimeArchiveWriter
    {
        private const int MetaReplaceAttempts = 5;

        // One lock per meta.json for the whole process, shared by every writer of the same directory.
        // The comparer ignores case so that two spellings of the same path on a case-insensitive
        // file system never end up with distinct locks.
        private static readonly ConcurrentDictionary<string, object> MetaLocks =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly string TagsName = GetMetaName(nameof(ArchiveMetaInformation.Tags));

        private static readonly string ResolvedEndPointsName =
            GetMetaName(nameof(ArchiveMetaInformation.ResolvedEndPoints));

        private static readonly string PropertiesName = GetMetaName(nameof(ArchiveMetaInformation.Properties));

        // What describes the capture session, as opposed to what belongs to the archive content.
        private static readonly string[] SessionNames = {
            GetMetaName(nameof(ArchiveMetaInformation.CapturedSetting)),
            GetMetaName(nameof(ArchiveMetaInformation.EnvironmentInformation)),
            GetMetaName(nameof(ArchiveMetaInformation.FluxzyVersion)),
            GetMetaName(nameof(ArchiveMetaInformation.ArchiveVersion))
        };

        // Content of a new meta.json, used when the directory has none (or an unreadable one).
        // Its Tags are the tags this writer already stored, which spares a file read for each
        // tagged exchange. The file is never rebuilt from this instance when it exists.
        private readonly ArchiveMetaInformation _archiveMetaInformation;
        private readonly object _metaLock;
        private readonly bool _describesSession;

        private static ArchiveMetaInformation CreateNewCaptureArchiveMetaInformation(FluxzySetting? capturedSetting)
        {
            var metaInformation = new ArchiveMetaInformation
            {
                EnvironmentInformation = new EnvironmentInformation(
                    RuntimeInformation.OSDescription,
#if NET6_0_OR_GREATER
                    RuntimeInformation.RuntimeIdentifier,
#else
                    "unknown",
#endif
                    FluxzySharedSetting.SkipCollectingEnvironmentInformation ? "" : Environment.MachineName
                ),
                CapturedSetting = FreezeCapturedSetting(capturedSetting)
            };

            return metaInformation;
        }

        private static FluxzySetting? FreezeCapturedSetting(FluxzySetting? capturedSetting)
        {
            if (capturedSetting == null)
                return null;

            // Snapshot the live setting so subsequent mutations don't leak into persisted meta.
            try {
                var json = JsonSerializer.Serialize(capturedSetting, GlobalArchiveOption.ConfigSerializerOptions);
                return JsonSerializer.Deserialize<FluxzySetting>(json, GlobalArchiveOption.ConfigSerializerOptions);
            }
            catch (Exception) {
                // A custom Action/Filter defined outside the Fluxzy.Core assembly cannot be
                // round-tripped by PolymorphicConverter. The snapshot is best-effort meta data,
                // so fall back to the live reference rather than failing proxy startup.
                return capturedSetting;
            }
        }

        private readonly string _archiveMetaInformationPath;
        private readonly string _baseDirectory;
        private readonly string _captureDirectory;
        private readonly string _contentDirectory;
        private readonly Filter? _saveFilter;
        private readonly string _errorDirectory;
        private readonly string _exchangeDirectory;
        private readonly string _connectionDirectory;

        public DirectoryArchiveWriter(string baseDirectory, Filter? saveFilter)
            : this(baseDirectory, saveFilter, capturedSetting: null)
        {
        }

        public DirectoryArchiveWriter(string baseDirectory, Filter? saveFilter, FluxzySetting? capturedSetting)
        {
            _archiveMetaInformation = CreateNewCaptureArchiveMetaInformation(capturedSetting);
            _describesSession = capturedSetting != null;
            _baseDirectory = baseDirectory;
            _saveFilter = saveFilter;
            _contentDirectory = Path.Combine(baseDirectory, "contents");
            _captureDirectory = Path.Combine(baseDirectory, "captures");
            _errorDirectory = Path.Combine(baseDirectory, "errors");
            _exchangeDirectory = Path.Combine(baseDirectory, "exchanges");
            _connectionDirectory = Path.Combine(baseDirectory, "connections");

            _archiveMetaInformationPath = DirectoryArchiveHelper.GetMetaPath(baseDirectory);

            _metaLock = MetaLocks.GetOrAdd(Path.GetFullPath(_archiveMetaInformationPath), _ => new object());
        }

        /// <summary>
        ///     Stores the endpoints the proxy is listening on in the archive meta information.
        /// </summary>
        public void SetResolvedEndPoints(IEnumerable<IPEndPoint> endPoints)
        {
            var resolvedEndPoints = endPoints.ToList();

            lock (_metaLock) {
                _archiveMetaInformation.ResolvedEndPoints = resolvedEndPoints;

                EditMeta(meta => {
                    meta[ResolvedEndPointsName] = JsonSerializer.SerializeToNode(resolvedEndPoints,
                        GlobalArchiveOption.DefaultSerializerOptions);

                    return true;
                });
            }
        }

        /// <summary>
        ///     Creates the directory layout and <c>meta.json</c>.
        ///     <para>
        ///         When the directory already holds a capture, its meta information is kept (capture
        ///         date, tags, view filters, properties). A writer that was given a captured setting
        ///         starts a new capture session on it, and replaces what describes the session:
        ///         <see cref="ArchiveMetaInformation.CapturedSetting" />,
        ///         <see cref="ArchiveMetaInformation.EnvironmentInformation" />,
        ///         <see cref="ArchiveMetaInformation.FluxzyVersion" /> and
        ///         <see cref="ArchiveMetaInformation.ArchiveVersion" />.
        ///     </para>
        /// </summary>
        public override void Init()
        {
            base.Init();

            _archiveMetaInformation.CaptureDate = DateTime.Now;

            Directory.CreateDirectory(_contentDirectory);
            Directory.CreateDirectory(_captureDirectory);
            Directory.CreateDirectory(_errorDirectory);
            Directory.CreateDirectory(_exchangeDirectory);
            Directory.CreateDirectory(_connectionDirectory);

            EditMeta(meta => _describesSession && SetSession(meta));
        }

        /// <summary>
        ///     Applies a change to <c>meta.json</c>, starting from the file on disk, and replaces the
        ///     file when the change modified it. The file is created when it does not exist yet.
        /// </summary>
        /// <param name="edit">Returns true when it modified the provided meta information</param>
        /// <returns>The value returned by <paramref name="edit" /></returns>
        private bool EditMeta(Func<JsonObject, bool> edit)
        {
            lock (_metaLock) {
                var existing = ReadMeta();
                var meta = existing ?? CreateMeta();
                var modified = edit(meta);

                if (existing == null || modified)
                    WriteMeta(meta);

                return modified;
            }
        }

        private JsonObject? ReadMeta()
        {
            if (!File.Exists(_archiveMetaInformationPath))
                return null;

            try {
                return JsonNode.Parse(File.ReadAllBytes(_archiveMetaInformationPath)) as JsonObject;
            }
            catch (JsonException) {
                // An empty or truncated file, left by a process that stopped while writing it.
                // The capture must go on, so the meta information starts over.
                return null;
            }
        }

        private JsonObject CreateMeta()
        {
            return JsonSerializer.SerializeToNode(_archiveMetaInformation,
                GlobalArchiveOption.DefaultSerializerOptions)!.AsObject();
        }

        private void WriteMeta(JsonObject meta)
        {
            // Written aside then moved, so that a reader never sees a partial file and a crash
            // never leaves a truncated one.
            var temporaryPath = _archiveMetaInformationPath + ".tmp";
            var options = GlobalArchiveOption.DefaultSerializerOptions;

            using (var fileStream = File.Create(temporaryPath)) {
                using var jsonWriter = new Utf8JsonWriter(fileStream, new JsonWriterOptions {
                    Indented = options.WriteIndented,
                    Encoder = options.Encoder
                });

                meta.WriteTo(jsonWriter, options);
            }

            for (var attempt = 1;; attempt++) {
                try {
                    File.Move(temporaryPath, _archiveMetaInformationPath, true);

                    return;
                }
                catch (Exception ex) when (attempt < MetaReplaceAttempts
                                           && (ex is IOException || ex is UnauthorizedAccessException)) {
                    // On Windows, the file cannot be replaced while a reader holds it open.
                    Thread.Sleep(15 * attempt);
                }
            }
        }

        private bool SetSession(JsonObject meta)
        {
            var session = CreateMeta();

            foreach (var name in SessionNames) {
                meta[name] = session[name]?.DeepClone();
            }

            return true;
        }

        private void AddTags(IEnumerable<Tag> tags)
        {
            lock (_metaLock) {
                List<Tag>? newTags = null;

                foreach (var tag in tags) {
                    if (!_archiveMetaInformation.Tags.Contains(tag))
                        (newTags ??= new List<Tag>()).Add(tag);
                }

                if (newTags == null)
                    return;

                EditMeta(meta => {
                    var storedTags = ReadTags(meta);
                    var modified = false;

                    foreach (var tag in newTags) {
                        modified = storedTags.Add(tag) || modified;
                    }

                    if (modified) {
                        meta[TagsName] = JsonSerializer.SerializeToNode(storedTags,
                            GlobalArchiveOption.DefaultSerializerOptions);
                    }

                    return modified;
                });

                // Only once the file holds them, so that a failed write is tried again.
                foreach (var tag in newTags) {
                    _archiveMetaInformation.Tags.Add(tag);
                }
            }
        }

        private static HashSet<Tag> ReadTags(JsonObject meta)
        {
            try {
                return meta[TagsName].Deserialize<HashSet<Tag>>(GlobalArchiveOption.DefaultSerializerOptions)
                       ?? new HashSet<Tag>();
            }
            catch (JsonException) {
                return new HashSet<Tag>();
            }
        }

        private static string GetMetaName(string propertyName)
        {
            return GlobalArchiveOption.DefaultSerializerOptions.PropertyNamingPolicy?.ConvertName(propertyName)
                   ?? propertyName;
        }

        /// <summary>
        ///     Adds the provided tags to the tags of the archive. Tags that the archive already holds
        ///     are left as they are.
        /// </summary>
        public override void UpdateTags(IEnumerable<Tag> tags)
        {
            AddTags(tags);
        }

        /// <inheritdoc />
        public override bool UpdateProperty(string key, string? value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (key.Length == 0)
                throw new ArgumentException("A property key cannot be empty", nameof(key));

            return EditMeta(meta => {
                var properties = meta[PropertiesName] as JsonObject;

                if (value == null)
                    return properties != null && properties.Remove(key);

                if (properties == null) {
                    properties = new JsonObject();
                    meta[PropertiesName] = properties;
                }

                if (properties[key] is JsonValue current
                    && current.TryGetValue<string>(out var currentValue)
                    && currentValue == value)
                    return false;

                properties[key] = value;

                return true;
            });
        }

        protected override bool ExchangeUpdateRequired(Exchange exchange)
        {
            if (_saveFilter != null && !_saveFilter.Apply(null, exchange.Authority, exchange, null))
                return false;

            return true;
        }

        protected override bool ConnectionUpdateRequired(Connection connection)
        {
            //if (_saveFilter != null && !_saveFilter.Apply(null, connection.Authority, null, null))
            //    return false;

            return true;
        }

        public override bool Update(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            var exchangePath = DirectoryArchiveHelper.GetExchangePath(_baseDirectory, exchangeInfo);

            DirectoryArchiveHelper.CreateDirectory(exchangePath);

            using (var fileStream = File.Create(exchangePath))
            {
                MessagePackSerializer.Serialize(fileStream, exchangeInfo,
                    GlobalArchiveOption.MessagePackSerializerOptions);

                // JsonSerializer.Serialize(fileStream, exchangeInfo, GlobalArchiveOption.DefaultSerializerOptions);
            }

            if (exchangeInfo.Tags?.Any() ?? false)
                AddTags(exchangeInfo.Tags);

            return true;
        }

        public override void Update(ConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            var connectionPath = DirectoryArchiveHelper.GetConnectionPath(_baseDirectory, connectionInfo);

            DirectoryArchiveHelper.CreateDirectory(connectionPath);

            using var fileStream = File.Create(connectionPath);

            MessagePackSerializer.Serialize(fileStream, connectionInfo,
                GlobalArchiveOption.MessagePackSerializerOptions, cancellationToken);
        }

        protected override void InternalUpdate(DownstreamErrorInfo errorInfo, CancellationToken cancellationToken)
        {
            var errorPath = DirectoryArchiveHelper.GetErrorPath(_baseDirectory);

            MessagePackQueueExtensions.AppendMultiple(errorPath, errorInfo,
                GlobalArchiveOption.MessagePackSerializerOptions);
        }

        public override Stream CreateRequestBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"req-{exchangeId}.data");

            return File.Create(path);
        }

        public override Stream CreateResponseBodyStream(int exchangeId)
        {
            var path = Path.Combine(_contentDirectory, $"res-{exchangeId}.data");

            return File.Create(path);
        }

        public override Stream CreateWebSocketRequestContent(int exchangeId, int messageId)
        {
            var path = Path.Combine(_contentDirectory, $"req-{exchangeId}-ws-{messageId}.data");

            return File.Open(path, FileMode.Append, FileAccess.Write);
        }

        public override Stream CreateWebSocketResponseContent(int exchangeId, int messageId)
        {
            var path = Path.Combine(_contentDirectory, $"res-{exchangeId}-ws-{messageId}.data");

            return File.Open(path, FileMode.Append, FileAccess.Write);
        }

        public override string GetDumpfilePath(int connectionId)
        {
            return Path.Combine(_captureDirectory, $"{connectionId}.pcapng");
        }

        public override void ClearErrors()
        {
            var errorPath = DirectoryArchiveHelper.GetErrorPath(_baseDirectory);

            ErrorCount = 0;

            if (File.Exists(errorPath))
                File.Delete(errorPath);
        }

        public (int ExchangeId, int ConnectionId) GetNextIds()
        {
            var maxExchangeIds =
                new DirectoryInfo(DirectoryArchiveHelper.GetExchangeDirectory(_baseDirectory))
                    .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .Select(x => {
                        var res = DirectoryArchiveHelper.TryParseIds(x.Name, out var ids);
                        return res ? ids.EndId : 0;
                    })
                    .OrderByDescending(r => r)
                    .DefaultIfEmpty(0)
                    .First();

            var maxConnectionIds =
                new DirectoryInfo(DirectoryArchiveHelper.GetConnectionDirectory(_baseDirectory))
                    .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .Select(x => {
                        var res = DirectoryArchiveHelper.TryParseIds(x.Name, out var ids);
                        return res ? ids.EndId : 0;
                    })
                    .OrderByDescending(r => r)
                    .DefaultIfEmpty(0)
                    .First();

            // Ids doesn't have to be contiguous, so we add a margin
            return (maxExchangeIds + 1, maxConnectionIds + 1);
        }

        public void WriteAsset(string relativePath, Stream stream)
        {
            var path = Path.Combine(_baseDirectory, relativePath);
            var fullPath = new FileInfo(path);

            fullPath.Directory?.Create();

            using var fileStream = File.Create(path);
            stream.CopyTo(fileStream);
        }
    }
}
