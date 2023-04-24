// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Fluxzy.Har;
using Fluxzy.Saz;

namespace Fluxzy
{
    public class PackagerInformationAttribute : Attribute
    {
        public PackagerInformationAttribute(
            string name, string description, string defaultExtension,
            params string[] extraExtensions)
        {
            Name = name;
            Description = description;
            DefaultExtension = defaultExtension;

            Extensions = new HashSet<string>(new[] { DefaultExtension }.Concat(extraExtensions),
                StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public string Description { get; }

        public string DefaultExtension { get; }

        public HashSet<string> Extensions { get; }
    }

    public static class AttributeExtensions
    {
        public static PackagerInformationAttribute GetInfo<T>(this T element)
            where T : DirectoryPackager
        {
            return element.GetType().GetCustomAttribute<PackagerInformationAttribute>()!;
        }
    }

    public class PackagerRegistry
    {
        public static PackagerRegistry Instance { get; } = new();

        public IReadOnlyCollection<DirectoryPackager> Packagers { get; }
            = new ReadOnlyCollection<DirectoryPackager>(new List<DirectoryPackager> {
                new FxzyDirectoryPackager(),
                new SazPackager(),
                new HttpArchivePackager()
            });

        public DirectoryPackager InferPackagerFromFileName(string fileName)
        {
            if (!Packagers.Any())
                throw new InvalidOperationException("No packager was registered yet");

            var extension = Path.GetExtension(fileName);

            foreach (var packager in Packagers) {
                var packagerInfo = packager.GetInfo();

                if (packagerInfo.Extensions.Contains(extension))
                    return packager;
            }

            return Packagers.First();
        }

        public DirectoryPackager GetPackageOrDefault(string name)
        {
            if (!Packagers.Any())
                throw new InvalidOperationException("No packager was registered yet");

            foreach (var packager in Packagers) {
                var packagerInfo = packager.GetInfo();

                if (packagerInfo.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return packager;
            }

            return Packagers.First();
        }
    }
}
