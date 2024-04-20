using System.Threading.Tasks;

namespace InstallerLib.Packages
{
    public interface IPackageCache
    {
        /// <summary>
        /// Adds a package to cache
        /// </summary>
        /// <param name="package">The package to add</param>
        Task AddPackageAsync(IPackage package);

        /// <summary>
        /// Deletes the package from the cache
        /// </summary>
        /// <param name="id"></param>
        Task DeletePackageAsync(string id);

        /// <summary>
        /// Retrieve from the cache
        /// </summary>
        /// <param name="id">The id of the package</param>
        /// <returns>Tthe package or null if not found</returns>
        Task<IPackage?> GetPackageAsync(string id);

        /// <summary>
        /// Tries to a find a package with the name and version
        /// </summary>
        /// <param name="name">Package name</param>
        /// <param name="version">Package version</param>
        /// <returns>The package id or null if not found</returns>
        Task<string?> FindPackageForVersionAsync(string name, string version);

        /// <summary>
        /// Returns the full path of the package
        /// </summary>
        /// <param name="id">The id of the package</param>
        /// <returns>The full path of the package or null if not found</returns>
        Task<string?> GetPackageFilePath(string id);
    }
}
