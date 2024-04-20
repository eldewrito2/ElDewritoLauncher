namespace TorrentLib
{
    public enum TorrentState
    {
        Unused,
        CheckingFiles,
        DownloadingMetadata,
        Downloading,
        Finished,
        Seeding,
        Allocating,
        CheckingResumeData
    };
}
