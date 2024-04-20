namespace TorrentLib
{
    public struct PeerRequest
    {
        public readonly int Piece;
        public readonly int Start;
        public readonly int Length;

        public PeerRequest(int piece, int start, int length)
        {
            Piece = piece;
            Start = start;
            Length = length;
        }
    }
}