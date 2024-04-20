using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EDLauncher.Core.Install
{
    public record FakeReleaseServiceOptions(string ReleaseInfoFilePath);

    public class FakeReleaseService : IReleaseService
    {
        private readonly ILogger<FakeReleaseService> _logger;
        private readonly FakeReleaseServiceOptions _options;

        public FakeReleaseService(ILogger<FakeReleaseService> logger, FakeReleaseServiceOptions options)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<ReleaseInfo?> GetLatestAsync(string releaseChannel, CancellationToken cancellationToken = default)
        {
            await Task.Delay(2000);
            string json = await File.ReadAllTextAsync(_options.ReleaseInfoFilePath).ConfigureAwait(false);
            ReleaseInfo releaseInfo = JsonSerializer.Deserialize<ReleaseInfo?>(json)!;
            if (releaseChannel != ReleaseInfo.GetChannel(releaseInfo.Version))
            {
                return null;
            }

            if (!releaseInfo.PackageUri.StartsWith("magnet:"))
            {
                string infohash = releaseInfo.PackageUri;
                releaseInfo = new ReleaseInfo(releaseInfo.Version, $"magnet:?xt=urn:btih:{infohash}&dn=eldewrito");
            }
            Debug.Assert(releaseInfo != null);
            _logger.LogWarning($"Using release info from '{_options.ReleaseInfoFilePath}' {json}");
            return releaseInfo;
        }

        public Task<ReleaseInfo?> GetUpdateAsync(string currentVersion, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
