using System;
using System.Collections.Generic;
using TorrentLib;

namespace EDLauncher.Core.Torrents
{
    class TorrentFileMapper
    {
        public record FileSlice(int FileIndex, long Start, long Length) { }

        private IList<FileEntry> _files;
        private long _totalSize;
        private long[] _fileOffsets;
        private long _pieceLength;

        public TorrentFileMapper(TorrentFile torrent)
        {
            _files = torrent.Info.Files;
            _pieceLength = torrent.Info.PieceLength;

            _fileOffsets = new long[_files.Count];
            long offset = 0;
            for (int i = 0; i < _files.Count; i++)
            {
                _fileOffsets[i] = offset;
                offset += _files[i].Length;
            }
            _totalSize = offset;
        }

        public List<FileSlice> MapBlock(int piece, long offset, long size)
        {
            if (piece < 0 || offset < 0 || size < 0)
            {
                throw new ArgumentException("Invalid input parameters. All values must be non-negative.");
            }

            // Calculate the starting offset of the piece
            long pieceOffset = piece * _pieceLength;

            // Find the file index that contains the starting offset of the requested block
            int fileIndex = Array.BinarySearch(_fileOffsets, pieceOffset + offset);

            // Adjust file index if BinarySearch returns a negative value
            // e.g if the offsets array does not actually contain the starting offset
            if (fileIndex < 0)
                fileIndex = ~fileIndex - 1;

            // Adjust the size if the block extends beyond the total size of the contiguous view
            if (pieceOffset > _totalSize - size)
                size = (int)(_totalSize - pieceOffset);

            var slices = new List<FileSlice>();
            long fileOffset = pieceOffset - _fileOffsets[fileIndex];

            // Iterate through the files and determine the slices that make up the requested block
            while (size > 0)
            {
                // Check if there is still data to be retrieved from the current file
                if (fileOffset < _files[fileIndex].Length)
                {
                    // Determine the length of the slice within the current file
                    long sliceLength = Math.Min(_files[fileIndex].Length - fileOffset, size);
                    // Add the slice to the list of slices
                    slices.Add(new FileSlice(fileIndex, fileOffset, sliceLength));
                    // Update the remaining size be retrieved
                    size -= sliceLength;
                    // Move to the next position within the file
                    fileOffset += sliceLength;
                }

                // move to the next file
                fileOffset -= _files[fileIndex].Length;
                ++fileIndex;
            }

            return slices;
        }

        public long GetPieceSize(int piece)
        {
            long offset = piece * _pieceLength;
            long size = _pieceLength;
            if (offset > _totalSize - _pieceLength)
                size = _totalSize - offset;
            return size;
        }
    }
}
