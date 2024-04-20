using System.Threading;
using System.Threading.Tasks;

namespace EDLauncher.Core.Install
{
    public interface IReleaseService
    {
        Task<ReleaseInfo?> GetLatestAsync(string releaseChannel = "", CancellationToken cancellationToken = default);
        //Task<ReleaseInfo?> GetUpdateAsync(string currentVersion, CancellationToken cancellationToken = default);
    }
}
