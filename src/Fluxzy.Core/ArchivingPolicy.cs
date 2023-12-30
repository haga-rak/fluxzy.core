// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    /// <summary>
    /// Archiving policy 
    /// </summary>
    public class ArchivingPolicy
    {
        /// <summary>
        /// Create a default archiving policy which archive nothing.
        /// </summary>
        [JsonConstructor]
        public ArchivingPolicy()
        {

        }

        /// <summary>
        /// Archiving policy type
        /// </summary>
        public ArchivingPolicyType Type { get; internal set; }

        /// <summary>
        /// If ArchivingPolicyType is Directory, this property will be set to the directory path.
        /// </summary>
        public string? Directory { get; internal set; }

        /// <summary>
        /// Default archiving policy which archive nothing.
        /// </summary>
        public static ArchivingPolicy None { get; } = new();

        /// <summary>
        /// Create archiving policy from a DirectoryInfo
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <returns></returns>
        public static ArchivingPolicy CreateFromDirectory(DirectoryInfo directoryInfo)
        {
            directoryInfo.Create();

            return new ArchivingPolicy {
                Directory = directoryInfo.FullName,
                Type = ArchivingPolicyType.Directory
            };
        }

        /// <summary>
        /// Create archiving policy from a directory path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ArchivingPolicy CreateFromDirectory(string path)
        {
            var directoryInfo = new DirectoryInfo(path);

            return CreateFromDirectory(directoryInfo);
        }
    }

    /// <summary>
    /// Archiving policy type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ArchivingPolicyType>))]
    public enum ArchivingPolicyType
    {
        /// <summary>
        /// No archiving
        /// </summary>
        None = 0,

        /// <summary>
        /// Write into directory
        /// </summary>
        Directory
    }
}
