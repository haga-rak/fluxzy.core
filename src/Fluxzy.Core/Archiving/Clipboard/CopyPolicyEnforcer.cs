// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;

namespace Fluxzy.Clipboard
{
    public class CopyPolicyEnforcer
    {
        private readonly CopyPolicy _policy;

        public CopyPolicyEnforcer(CopyPolicy policy)
        {
            _policy = policy;
        }

        private static CopyArtefact? BuildArtefact(ArchiveAsset archiveAsset, CopyOptionType copyOptionType)
        {
            using var stream = archiveAsset.Open();

            if (copyOptionType == CopyOptionType.Memory) {
                return new CopyArtefact(
                    archiveAsset.RelativeName,
                    Path.GetExtension(archiveAsset.RelativeName),
                    stream.ReadMaxLengthOrNull(int.MaxValue),
                    null);
            }

            // Original archive cannot be copied by reference 

            if (archiveAsset.FullPath == null) {
                return null;
            }

            return new CopyArtefact(
                archiveAsset.RelativeName,
                Path.GetExtension(archiveAsset.RelativeName),
                null,
                archiveAsset.FullPath);
        }

        public virtual CopyArtefact? Get(ArchiveAsset archiveAsset)
        {
            if (archiveAsset.RelativeName.EndsWith(".mpack", StringComparison.OrdinalIgnoreCase)) {
                // always copy mpack files
                return BuildArtefact(archiveAsset, CopyOptionType.Memory);
            }

            if (_policy.DisallowedExtensions != null && _policy.DisallowedExtensions.Any(e =>
                    archiveAsset.RelativeName.EndsWith(e, StringComparison.OrdinalIgnoreCase))) {
                return null; // disallowed extension
            }

            if (_policy.Type == CopyOptionType.Memory) {
                if (_policy.MaxSize != null && archiveAsset.Length > _policy.MaxSize) {
                    return null; // too big
                }

                return BuildArtefact(archiveAsset, CopyOptionType.Memory);
            }

            if (_policy.Type == CopyOptionType.Reference) {
                return BuildArtefact(archiveAsset, CopyOptionType.Reference);
            }

            throw new InvalidOperationException("Unknown copy option type");
        }
    }
}
