using System.Collections;
using System.Runtime.InteropServices;

namespace TorrentLib
{
    public struct SHA1_Hash : IEquatable<SHA1_Hash>, IComparable<SHA1_Hash>, IReadOnlyList<byte>
    {
        public const int Length = 20;

        private readonly byte[] _bytes = new byte[Length];

        public static readonly SHA1_Hash Empty = new SHA1_Hash();

        public SHA1_Hash()
        {
        }

        public SHA1_Hash(ReadOnlySpan<byte> bytes)
        {
            bytes.CopyTo(_bytes);
        }

        public SHA1_Hash(byte[] bytes)
        {
            if (bytes.Length != Length)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            Array.Copy(bytes, _bytes, Length);
        }

        public unsafe SHA1_Hash(byte* bytes)
        {
            if (bytes == null)
                throw new NullReferenceException(nameof(bytes));

            Marshal.Copy((IntPtr)bytes, _bytes, 0, Length);
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            return new ReadOnlySpan<byte>(_bytes);
        }

        public override string ToString()
        {
            return Convert.ToHexString(_bytes).ToLowerInvariant();
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[Length];
            Array.Copy(_bytes, result, Length);
            return result;
        }

        public static bool TryParse(string str, out SHA1_Hash hash)
        {
            try
            {
                hash = new SHA1_Hash(Convert.FromHexString(str));
                return true;
            }
            catch
            {
                hash = Empty;
                return false;
            }
        }

        public static SHA1_Hash Parse(string str)
        {
            try
            {
                return new SHA1_Hash(Convert.FromHexString(str));

            }
            catch (Exception ex)
            {
                throw new FormatException("The input string was not in the correct format", ex);
            }
        }

        public static bool operator ==(SHA1_Hash a, SHA1_Hash b)
        {
            return ReferenceEquals(a._bytes, b._bytes) || a._bytes.SequenceEqual(b._bytes);
        }

        public static bool operator !=(SHA1_Hash a, SHA1_Hash b)
        {
            return !(a == b);
        }

        public override bool Equals(object? obj)
        {
            return obj is SHA1_Hash a && Equals((SHA1_Hash)obj);
        }

        public bool Equals(SHA1_Hash other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return _bytes.GetHashCode();
        }

        public int CompareTo(SHA1_Hash other)
        {
            for (int i = 0; i < Length; i++)
            {
                byte b1 = _bytes[i];
                byte b2 = other._bytes[i];
                if (b1 != b2)
                    return b1 < b2 ? -1 : 1;
            }
            return 0;
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => (IEnumerator<byte>)_bytes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<byte>)this).GetEnumerator();

        int IReadOnlyCollection<byte>.Count => Length;

        public byte this[int index] => _bytes[index];
    }

    public struct SHA256_Hash : IEquatable<SHA256_Hash>, IComparable<SHA256_Hash>, IReadOnlyList<byte>
    {
        public const int Length = 32;

        private readonly byte[] _bytes = new byte[Length];

        public static readonly SHA256_Hash Empty = new SHA256_Hash();

        public SHA256_Hash()
        {
        }

        public SHA256_Hash(ReadOnlySpan<byte> bytes)
        {
            bytes.CopyTo(_bytes);
        }

        public SHA256_Hash(byte[] bytes)
        {
            if (bytes.Length != Length)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            Array.Copy(bytes, _bytes, Length);
        }

        public unsafe SHA256_Hash(byte* bytes)
        {
            Marshal.Copy((IntPtr)bytes, _bytes, 0, Length);
        }

        public override string ToString()
        {
            return Convert.ToHexString(_bytes);
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[Length];
            Array.Copy(_bytes, result, Length);
            return result;
        }

        public static bool TryParse(string str, out SHA256_Hash hash)
        {
            try
            {
                hash = new SHA256_Hash(Convert.FromHexString(str));
                return true;
            }
            catch
            {
                hash = Empty;
                return false;
            }
        }

        public static SHA256_Hash Parse(string str)
        {
            try
            {
                return new SHA256_Hash(Convert.FromHexString(str));

            }
            catch (Exception ex)
            {
                throw new FormatException("The input string was not in the correct format", ex);
            }
        }

        public static bool operator ==(SHA256_Hash a, SHA256_Hash b)
        {
            return ReferenceEquals(a._bytes, b._bytes) || a._bytes.SequenceEqual(b._bytes);
        }

        public static bool operator !=(SHA256_Hash a, SHA256_Hash b)
        {
            return !(a == b);
        }

        public override bool Equals(object? obj)
        {
            return obj is SHA256_Hash a && Equals((SHA256_Hash)obj);
        }

        public bool Equals(SHA256_Hash other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return _bytes.GetHashCode();
        }

        public int CompareTo(SHA256_Hash other)
        {
            for (int i = 0; i < Length; i++)
            {
                byte b1 = _bytes[i];
                byte b2 = other._bytes[i];
                if (b1 != b2)
                    return b1 < b2 ? -1 : 1;
            }
            return 0;
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => (IEnumerator<byte>)_bytes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<byte>)this).GetEnumerator();

        int IReadOnlyCollection<byte>.Count => Length;

        public byte this[int index] => _bytes[index];
    }
}
