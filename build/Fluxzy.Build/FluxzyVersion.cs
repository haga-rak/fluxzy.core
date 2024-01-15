// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.RegularExpressions;

namespace Fluxzy.Build
{
    internal class FluxzyVersion
    {
        public FluxzyVersion(string tagName, string shortVersion, bool isCli, string friendlyVersionName, Version version)
        {
            TagName = tagName;
            ShortVersion = shortVersion;
            IsCli = isCli;
            FriendlyVersionName = friendlyVersionName;
            Version = version;
        }

        /// <summary>
        ///   Equals to tag name
        /// </summary>
        public string TagName { get; }

        /// <summary>
        ///  Just MAJOR.MINOR.PATCH
        /// </summary>
        public string ShortVersion { get; }

        /// <summary>
        ///  Like short version but with prefix v
        /// </summary>
        public string FriendlyVersionName { get; }

        /// <summary>
        ///   true if it's a cli version
        /// </summary>
        public bool IsCli { get; }

        /// <summary>
        ///  
        /// </summary>
        public Version Version { get; }

        public string ReleaseBranch => $"release/v{Version.Major}.{Version.Minor}";

        public override string ToString()
        {
            return TagName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawVersion"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static bool TryParse(string rawVersion, out FluxzyVersion version)
        {
            version = default!;

            if (!rawVersion.StartsWith("v"))
                return false;

            var isCli = rawVersion.EndsWith("-cli");

            // v1.17.8.57173-cli

            var regex = new Regex(@"^v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.(?<build>\d+)(?<cli>-cli)?$");

            var match = regex.Match(rawVersion);

            if (!match.Success)
                return false;

            var major = int.Parse(match.Groups["major"].Value);
            var minor = int.Parse(match.Groups["minor"].Value);
            var patch = int.Parse(match.Groups["patch"].Value);
            var build = int.Parse(match.Groups["build"].Value);

            var shortVersion = $"{major}.{minor}.{patch}";
            var friendlyVersionName = $"v{shortVersion}";

            if (isCli)
            {
                shortVersion += "-cli";
                friendlyVersionName += "-cli";
            }

            version = new FluxzyVersion(rawVersion, shortVersion, isCli, friendlyVersionName,
                new Version(major, minor, patch, build));

            return true;
        }
    }
}
