// Copyright © 2022 Haga Rakotoharivelo

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

        public static ArchivingPolicy CreateFromDirectory(string path)
        {
            return new ArchivingPolicy()
            {
                Type = ArchivingPolicyType.Directory,
                Directory = path
            }; 
        }
    }

    public enum ArchivingPolicyType
    {
        // The proxy 
        None = 0,
        
        Directory
    }
}