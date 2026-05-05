// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Fluxzy.Rules.Filters;

namespace Fluxzy
{
    /// <summary>
    /// Meta information about an archive.
    /// </summary>
    public class ArchiveMetaInformation
    {
        /// <summary>
        /// The date when the archive was created.
        /// </summary>
        public DateTime CaptureDate { get; set; } = DateTime.Now;

        /// <summary>
        /// List of available tags on this archive.
        /// </summary>
        public HashSet<Tag> Tags { get; set; } = new();

        /// <summary>
        /// List of available filters on this archive.
        /// </summary>
        public List<Filter> ViewFilters { get; set; } = new();

        /// <summary>
        /// Archive version
        /// </summary>
        public string ArchiveVersion { get; set; } = "0.3.0";

        /// <summary>
        ///  Information about the environment where the archive was created.
        /// </summary>
        public EnvironmentInformation? EnvironmentInformation { get; set; }

        /// <summary>
        /// Fluxzy version used to create this archive
        /// </summary>
        public string FluxzyVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        /// <summary>
        ///     Snapshot of the <see cref="FluxzySetting"/> that was active when the archive was produced.
        ///     Sensitive fields (PKCS#12 password and file path, proxy authentication password,
        ///     certificate cache directory, user agent configuration file) are scrubbed before serialization.
        ///     When <see cref="FluxzySharedSetting.RedactSettingsInArchive"/> is true, alteration rules
        ///     and the save filter are also omitted.
        ///     Null on archives produced by Fluxzy &lt;= 0.2.0 and on archives produced by import engines.
        /// </summary>
        public FluxzySetting? CapturedSetting { get; set; }

        /// <summary>
        ///     Endpoints the proxy was actually listening on once <c>Proxy.Run()</c> resolved them.
        ///     Differs from <see cref="FluxzySetting.BoundPoints"/> when a configured port of 0
        ///     was replaced by an OS-assigned ephemeral port.
        /// </summary>
        public List<IPEndPoint> ResolvedEndPoints { get; set; } = new();

        /// <summary>
        ///  Can be used to store additional information about the archive.
        /// </summary>

        public Dictionary<string, string> Properties { get; set; } = new();
    }
}
