using System.Collections.Generic;

namespace InstallerLib.Packages
{
    record Manifest(string Name, string Version, Dictionary<string, string> Files);

    record struct ManifestFileEntry(string Path, long Size, string Sha1);
}
