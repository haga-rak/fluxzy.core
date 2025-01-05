// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Clients.Ssl
{
    public class ImpersonateAgent
    {
        public ImpersonateAgent(string name, string platform, string version)
        {
            Name = name;
            Version = version;
            Platform = platform;
        }

        public string Name { get; }

        public string Version { get; }

        public Version? VersionAsVersion
        {
            get
            {
                return System.Version.TryParse(Version, out var version) ? version : null;
            }
        }

        public string Platform { get; }

        public bool Latest => string.Equals(Version, "latest", StringComparison.OrdinalIgnoreCase);

        public bool Absolute => !Latest;


        protected bool Equals(ImpersonateAgent other)
        {
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Platform, other.Platform, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ImpersonateAgent)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Version, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Platform, StringComparer.OrdinalIgnoreCase);

            return hashCode.ToHashCode();
        }

        public string ToFlatName()
        {
            return $"{Name}_{Platform}_{Version}";
        }

        public override string ToString()
        {
            return ToFlatName();
        }

        public static bool TryParse(string rawString, out ImpersonateAgent result)
        {
            result = null!;

            if (string.IsNullOrWhiteSpace(rawString))
                return false;

            var parts = rawString.Split('_');

            if (parts.Length != 3)
                return false;

            result = new ImpersonateAgent(parts[0], parts[1], parts[2]);

            return true;
        }

        public static ImpersonateAgent Parse(string rawString)
        {
            if (!TryParse(rawString, out var result))
            {
                throw new ArgumentException("Invalid format", nameof(rawString));
            }

            return result;
        }
    }
}
