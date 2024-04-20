using NuGet.Versioning;
using System.Collections.Generic;

namespace InstallerLib.Packages
{
    public record struct PackageFileEntry
    (
        string Path,
        long FileSize
    );

    public interface IPackage
    {
        public string Id { get; }

        public string Uri { get; }

        public string Name { get; }

        public SemanticVersion Version { get; }

        public List<PackageFileEntry> Files { get; }
    }
}
