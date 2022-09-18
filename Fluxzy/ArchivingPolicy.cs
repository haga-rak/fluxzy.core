// Copyright © 2022 Haga Rakotoharivelo

using System.IO;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    public class ArchivingPolicy
    {
        [JsonConstructor]
        internal ArchivingPolicy()
        {

        }

        public ArchivingPolicyType Type { get; internal set; }

        public string Directory { get; internal set; }

        public static ArchivingPolicy None { get; } = new();

        public static ArchivingPolicy CreateFromDirectory(DirectoryInfo directoryInfo)
        {
            directoryInfo.Create();
            return new ArchivingPolicy()
            {
                Directory = directoryInfo.FullName,
                Type = ArchivingPolicyType.Directory
            };
        }
        public static ArchivingPolicy CreateFromDirectory(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            return CreateFromDirectory(directoryInfo);
        }
    }

    public enum ArchivingPolicyType
    {
        // The proxy 
        None = 0,
        Directory
    }
}