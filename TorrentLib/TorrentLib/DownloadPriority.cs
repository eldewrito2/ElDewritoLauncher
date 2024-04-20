namespace TorrentLib
{
    public enum DownloadPriority
    {
        /// <summary>
        /// Don't download the file or piece. Partial pieces may still be downloaded when setting file priorities.
        /// </summary>
        DontDownload = 0,

        /// <summary>
        /// The lowest priority for files and pieces.
        /// </summary>
        Low = 1,

        /// <summary>
        /// The default priority for files and pieces.
        /// </summary>
        /// 
        Default = 4,

        /// <summary>
        /// The highest priority for files and pieces.
        /// </summary>
        Top = 7
    }
}