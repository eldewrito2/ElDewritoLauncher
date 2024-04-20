namespace TorrentLib
{
    /// <summary>
    /// Provides a <see cref="Stream"/> for a file in a torrent
    /// </summary>
    public class TorrentFileStream : Stream
    {
        public const int PieceBufferCount = 5;

        private Session _session;
        private int _torrentId;
        private int _fileIndex;
        private long _position;
        private long _startOffset;
        private long _endOffset;
        private int _pieceCursor;
        private int _endPiece;
        private int _firstPiece;
        private int _pieceOffset;
        private int _startPieceOffset;
        private Queue<PieceRequest> _requests = new();

        /// <param name="session">The session</param>
        /// <param name="torrentId">The id of the torrent</param>
        /// <param name="fileIndex">The file index in the torrent info</param>
        /// <param name="startOffset">The offset to start reading from</param>
        /// <param name="size">The max number of bytes that can be read or -1 to use the file size</param>
        public TorrentFileStream(Session session, int torrentId, int fileIndex, long startOffset = 0, long size = -1)
        {
            _session = session;
            _torrentId = torrentId;
            _fileIndex = fileIndex;
            _position = startOffset;

            var files = session.GetTorrentFileEntries(torrentId);
            if (size == -1)
                size = files[fileIndex].Length;

            var pr1 = session.MapFileRange(torrentId, fileIndex, startOffset, 0);
            var pr2 = session.MapFileRange(torrentId, fileIndex, startOffset + size - 1, 0);

            _startOffset = startOffset;
            _endOffset = startOffset + size;
            _firstPiece = pr1.Piece;
            _endPiece = pr2.Piece + 1;
            _startPieceOffset = pr1.Start;
            _pieceOffset = _startPieceOffset;
            _pieceCursor = _firstPiece;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _endOffset - _startOffset;

        public override long Position { get => _position; set => throw new NotImplementedException(); }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            BufferPieces();

            int bytesRead = 0;
            while (_position < _endOffset)
            {
                if (_requests.Count == 0)
                    throw new Exception("End of stream");

                var req = _requests.Peek();
                var piece = req.Task.GetAwaiter().GetResult();
                int n = ReadFromPiece(piece, buffer, ref offset, ref count);
                if (n == 0)
                    break;
                bytesRead += n;
            }

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken)
        {
            BufferPieces(cancelToken);

            int bytesRead = 0;

            while (_position < _endOffset && !cancelToken.IsCancellationRequested)
            {
                if (_requests.Count == 0)
                    throw new Exception("End of stream");

                var req = _requests.Peek();
                var piece = await req.Task;
                int n = ReadFromPiece(piece, buffer, ref offset, ref count);
                if (n == 0)
                    break;

                bytesRead += n;
            }

            cancelToken.ThrowIfCancellationRequested();
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return SeekAbsolute(
                origin switch
                {
                    SeekOrigin.Begin => _startOffset + offset,
                    SeekOrigin.End => _endOffset - offset,
                    SeekOrigin.Current => _position + offset,
                    _ => throw new NotImplementedException(),
                });
        }

        private long SeekAbsolute(long offset)
        {
            if (offset < _startOffset || offset > _endOffset)
                throw new ArgumentOutOfRangeException(nameof(offset));

            var pr1 = _session.MapFileRange(_torrentId, _fileIndex, offset, 0);
            while (_requests.Count > 0 && _requests.Peek().Piece < pr1.Piece)
                _requests.Dequeue();

            _pieceCursor = pr1.Piece;
            _pieceOffset = pr1.Start;
            _position = offset;
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        private void BufferPieces(CancellationToken cancelToken = default)
        {
            while (_requests.Count < PieceBufferCount && _pieceCursor < _endPiece)
            {
                var fetchTask = _session.ReadPieceAsync(_torrentId, _pieceCursor, cancelToken);
                _requests.Enqueue(new PieceRequest(_pieceCursor, fetchTask));
                _pieceCursor++;
            }
        }

        private int ReadFromPiece(byte[] piece, byte[] buffer, ref int offset, ref int count)
        {
            int n = Math.Min(count, piece.Length - _pieceOffset);
            if (_endOffset - _position < n)
                n = (int)(_endOffset - _position);

            if (n == 0)
                return 0;

            Array.Copy(piece, _pieceOffset, buffer, offset, n);

            count -= n;
            offset += n;
            _pieceOffset += n;
            _position += n;

            if (_pieceOffset == piece.Length || _position == _endOffset)
            {
                _pieceOffset = 0;
                _requests.Dequeue();
            }

            return n;
        }

        class PieceRequest
        {
            public int Piece;
            public Task<byte[]> Task;

            public PieceRequest(int piece, Task<byte[]> task)
            {
                Piece = piece;
                Task = task;
            }
        }
    }
}
