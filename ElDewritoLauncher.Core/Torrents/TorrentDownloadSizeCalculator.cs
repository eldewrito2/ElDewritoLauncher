using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TorrentLib;
using static EDLauncher.Core.Torrents.TorrentFileMapper;

namespace EDLauncher.Core.Torrents
{
    public class TorrentDownloadSizeCalculcator
    {
        private TorrentInfo info;
        private TorrentFileMapper mapper;

        public TorrentDownloadSizeCalculcator(TorrentFile torrent)
        {
            info = torrent.Info;
            mapper = new TorrentFileMapper(torrent);
        }

        public Task<long> CalculcateAsync(string directory, Dictionary<string, string>? renamedFiles)
        {
            return Task.Run(() => Calculcate(directory, renamedFiles));
        }

        public Task<long> Calculcate(string directory, Dictionary<string, string>? renamedFiles)
        {
            bool[] invalidPieces = GetInvalidPieces(directory, renamedFiles);
            long size = 0;
            for (int i = 0; i < invalidPieces.Length; i++)
            {
                if (invalidPieces[i])
                    size += mapper.GetPieceSize(i);
            }
            return Task.FromResult(size);
        }

        public bool[] GetInvalidPieces(string directory, Dictionary<string, string>? renamedFiles)
        {
            bool[] invalidPieces = new bool[info.PieceHashes.Count];

            Parallel.For(0, info.PieceHashes.Count, piece =>
            {
                using SHA1 hasher = SHA1.Create();
                byte[] pieceBuf = new byte[info.PieceLength];
                List<FileSlice> slices = mapper.MapBlock(piece, 0, info.PieceLength);

                foreach (FileSlice slice in slices)
                {
                    string filePath = GetFileFullPath(directory, renamedFiles, info.Files[slice.FileIndex].Path);
                    if (!File.Exists(filePath))
                        continue;

                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192);
                    fs.Seek(slice.Start, SeekOrigin.Begin);
                    int bytesRead = fs.Read(pieceBuf, 0, (int)slice.Length);
                    hasher.TransformBlock(pieceBuf, 0, bytesRead, null, 0);
                }
                hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                invalidPieces[piece] = !hasher.Hash.AsSpan().SequenceEqual(info.PieceHashes[piece].AsSpan());
            });

            return invalidPieces;
        }

        private static string GetFileFullPath(string directory, Dictionary<string, string>? renamedFiles, string path)
        {
            if (renamedFiles != null && renamedFiles.TryGetValue(path, out string? renamedPath))
            {
                path = renamedPath;
            }
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(directory, path));
            }
            return path;
        }
    }
}
