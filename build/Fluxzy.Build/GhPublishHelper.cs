// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text;
using Octokit;

namespace Fluxzy.Build
{
    internal class GhPublishHelper
    {
        private readonly Func<GitHubClient> _clientBuilder;
        private readonly long _repositoryId;

        private GhPublishHelper(Func<GitHubClient> clientBuilder, long repositoryId)
        {
            _clientBuilder = clientBuilder;
            _repositoryId = repositoryId;
        }

        public static string BodyNuGet { get; } = @"
Official .NET builds are *signed* and published at [nuget.org](https://www.nuget.org/profiles/Fluxzy).

[![v{0}](https://img.shields.io/badge/Fluxzy.Core-{0}-blue)](https://www.nuget.org/packages/Fluxzy.Core/{1}) [![v{0}](https://img.shields.io/badge/Fluxzy.Core.Pcap-{0}-blue)](https://www.nuget.org/packages/Fluxzy.Core.Pcap/{1})

- CLI binaries are available as assets or downloadable via [this page](https://www.fluxzy.io/download#cli). 
- Docker images are available at [Docker Hub](https://hub.docker.com/r/fluxzy/fluxzy).

";

        public static async Task<GhPublishHelper> Create(string token, string userName, string repositoryName)
        {
            GitHubClient Factory()
            {
                var client = new GitHubClient(new ProductHeaderValue(nameof(GhPublishHelper)));

                client.Credentials = new Credentials(token);

                return client;
            }

            var client = Factory();

            var repository = await client.Repository.Get(userName, repositoryName);
            var instance = new GhPublishHelper(Factory, repository.Id);

            return instance;
        }


        public async Task DeleteAllPendingDrafts()
        {
            var client = _clientBuilder();

            var existing = (await client.Repository.Release.GetAll(_repositoryId))
                           .Where(r => r.Draft)
                           .ToList();


            var tasks = new List<Task>();

            foreach (var release in existing) tasks.Add(client.Repository.Release.Delete(_repositoryId, release.Id));

            await Task.WhenAll(tasks);
        }

        public async Task<List<FluxzyVersion>> ReadAvailableTags()
        {
            var client = _clientBuilder();

            return (await client.Repository.GetAllTags(_repositoryId))
                   .Select(s =>
                   {
                       FluxzyVersion.TryParse(s.Name, out var version);
                       return version;
                   })
                   .Where(s => s != null! && !s.IsCli)
                   .OrderBy(s => s!.Version)
                   .ToList();
        }

        internal async Task<bool> DeleteExisting(string tagName)
        {
            var client = _clientBuilder();

            var existingReleases = await client.Repository.Release.GetAll(_repositoryId);

            var items = existingReleases.Where(r => r.TagName == tagName).ToList();

            foreach (var item in items) await client.Repository.Release.Delete(_repositoryId, item.Id);

            return true;
        }

        public async Task<bool> Publish(string tagName)
        {
            if (!FluxzyVersion.TryParse(tagName, out var currentVersion))
                throw new ArgumentException("Invalid tag name", nameof(tagName));

            await DeleteExisting(tagName);

            var productionTags = await ReadAvailableTags();

            if (productionTags.All(t => t.Version != currentVersion.Version))
                throw new ArgumentException($"Tag {tagName} does not exists", nameof(tagName));

            var previousVersion = productionTags
                                  .Where(t => !t.IsCli && t.Version < currentVersion.Version).MaxBy(t => t.Version);

            if (previousVersion == null) 
                throw new ArgumentException("No previous version found", nameof(tagName));

            await InternalPublish(previousVersion.TagName, currentVersion, Array.Empty<FileInfo>());

            return true;
        }

        internal static string GetUntilReleaseVersionName(string version)
        {
            var parts = version.Split('.');
            return string.Join('.', parts.Take(3));
        }
        
        
        public async Task<bool> AddAssets(string tagName, IEnumerable<FileInfo> fileInfos, bool addHash = true)
        {
            var client = _clientBuilder();

            var existingReleases = await client.Repository.Release.GetAll(_repositoryId);
            var release = existingReleases.FirstOrDefault(r => string.Equals(GetUntilReleaseVersionName(r.TagName),
                GetUntilReleaseVersionName(tagName), StringComparison.OrdinalIgnoreCase));

            if (release == null)
                throw new InvalidOperationException($"Tag {tagName} does not exists." +
                                                    $"NuGet package has to be published before adding assets");

            // Get existing assets to skip already uploaded files
            var existingAssets = await client.Repository.Release.GetAllAssets(_repositoryId, release.Id);
            var existingAssetNames = existingAssets.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var fileInfo in fileInfos)
            {
                if (existingAssetNames.Contains(fileInfo.Name))
                {
                    Console.WriteLine($"Skipping {fileInfo.Name} - already exists");
                    continue;
                }

                using var stream = fileInfo.OpenRead();

                var assetUpload = new ReleaseAssetUpload(fileInfo.Name,
                    "application/octet-stream", stream, null);

                await client.Repository.Release.UploadAsset(release, assetUpload);
                Console.WriteLine($"Uploaded {fileInfo.Name}");

                if (addHash)
                {
                    var hashFileName = $"{fileInfo.Name}.sha256";

                    if (existingAssetNames.Contains(hashFileName))
                    {
                        Console.WriteLine($"Skipping {hashFileName} - already exists");
                        continue;
                    }

                    using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(HashHelper.GetWinGetHash(fileInfo)));

                    var hashPayload = new ReleaseAssetUpload(hashFileName,
                        "text/plain", memoryStream, null);

                    await client.Repository.Release.UploadAsset(release, hashPayload);
                    Console.WriteLine($"Uploaded {hashFileName}");
                }
            }

            return true;
        }

        public async Task<bool> InternalPublish(string previousTagName, FluxzyVersion version,
            IEnumerable<FileInfo> assets)
        {
            var client = _clientBuilder();

            if (version.IsCli)
                throw new InvalidOperationException("Only for nuget package");

            var tagName = version.TagName;

            var releaseData = new NewRelease(tagName)
            {
                GenerateReleaseNotes = false,
                Body = string.Format(BodyNuGet, version.FriendlyVersionName, version.ShortVersion),
                Draft = true,
                Prerelease = false,
                Name = "Fluxzy " + version.FriendlyVersionName,
                TargetCommitish = "main"
            };

            var release = await client.Repository.Release.Create(_repositoryId, releaseData);

            var generateReleaseNoteRequestData = new GenerateReleaseNotesRequest(tagName)
            {
                PreviousTagName = previousTagName,
                TargetCommitish = "main"
            };

            var releaseNote =
                await client.Repository.Release.GenerateReleaseNotes(_repositoryId, generateReleaseNoteRequestData);

            var finalNote = releaseNote.Body;

            var fullLines = finalNote.Split(new[] { "\n" }, StringSplitOptions.None)
                                     .Where(r =>
                                         !r.Contains("README.md", StringComparison.OrdinalIgnoreCase) &&
                                         !r.Contains("test", StringComparison.OrdinalIgnoreCase)
                                     )
                                     .ToList();

            finalNote = string.Join("\n", fullLines);

            var releaseUpdateData = new ReleaseUpdate
            {
                Body = release.Body + "\n" + finalNote,
                TagName = release.TagName,
                TargetCommitish = release.TargetCommitish,
                Name = release.Name,
                Draft = release.Draft,
                Prerelease = release.Prerelease
            };

            await client.Repository.Release.Edit(_repositoryId, release.Id, releaseUpdateData);
            return false;
        }
    }
}
