using System.Collections;
using System.Text;
using System.Linq;

namespace TorrentLib
{
    public static class Bencode
    {
        // all inputs and ouputs use extended ascii encoding
        public static readonly Encoding Encoding = Encoding.GetEncoding("iso-8859-1");

        public static byte[] Encode(object input)
        {
            var output = new MemoryStream();
            using (var s = new StreamWriter(output, Encoding))
                Encode(s, input);
            return output.ToArray();
        }

        public static unsafe object? Decode(byte[] input)
        {
            return Decode(input, 0, input.Length);
        }

        public static object? Decode(byte[] input, int offset, int count)
        {
            using (var s = new StreamReader(new MemoryStream(input, offset, count), Encoding))
                return Decode(s);
        }

        public static unsafe object? Decode(byte* input, int size)
        {
            using (var s = new StreamReader(new UnmanagedMemoryStream(input, size), Encoding))
                return Decode(s);
        }

        public static object? Decode(TextReader reader)
        {
            char c = (char)reader.Peek();
            if (c == char.MaxValue) return null;
            if (c == 'i') return ReadInt();
            else if (c == 'l') return ReadList();
            else if (c == 'd') return ReadDict();
            else return ReadString();

            int ReadLength()
            {
                var buff = new StringBuilder();
                while (true)
                {
                    char c = (char)reader.Read();
                    if (c == char.MaxValue) throw new FormatException();
                    if (c == ':') break;
                    if (!char.IsDigit(c)) throw new FormatException();
                    buff.Append(c);
                }
                return int.Parse(buff.ToString());
            }

            long ReadInt()
            {
                if (reader.Read() != 'i') throw new FormatException();
                var buff = new StringBuilder();
                while (true)
                {
                    char c = (char)reader.Read();
                    if (c == char.MaxValue) throw new FormatException();
                    if (c == 'e') break;
                    if (!char.IsDigit(c) && (buff.Length != 0 || c != '-')) throw new FormatException();
                    buff.Append(c);
                }
                return long.Parse(buff.ToString());
            }

            string ReadStringN(int length)
            {
                var buff = new StringBuilder();
                while (length-- > 0)
                {
                    char c = (char)reader.Read();
                    if (c == char.MaxValue) throw new FormatException();
                    buff.Append(c);
                }
                return buff.ToString();
            }

            string ReadString()
            {
                int length = ReadLength();
                return ReadStringN(length);
            }

            IList ReadList()
            {
                if (reader.Read() != 'l') throw new FormatException();
                var list = new List<object?>();
                while (true)
                {
                    char c = (char)reader.Peek();
                    if (c == char.MaxValue) throw new FormatException();
                    if (c == 'e') break;
                    list.Add(Decode(reader));
                }
                reader.Read();
                return list;
            }

            Dictionary<string, object?> ReadDict()
            {
                if (reader.Read() != 'd') throw new FormatException();
                var dict = new Dictionary<string, object?>();
                while (true)
                {
                    char c = (char)reader.Peek();
                    if (c == char.MaxValue) throw new FormatException();
                    if (c == 'e') break;
                    string key = ReadString();
                    object? value = Decode(reader);
                    dict[key] = value;
                }
                reader.Read();
                return dict;
            }
        }

        public static void Encode(TextWriter writer, object input)
        {
            switch (input)
            {
                case sbyte:
                case short:
                case int:
                case long:
                    writer.Write($"i{input}e");
                    break;
                case bool b:
                    Encode(writer, b ? 1 : 0);
                    break;
                case string s:
                    // "The specification does not deal with encoding of characters outside the ASCII set"
                    writer.Write($"{s.Length}:{s}");
                    break;
                case IList list:
                    writer.Write("l");
                    foreach (var element in list)
                        Encode(writer, element);
                    writer.Write("e");
                    break;
                case IDictionary dict:
                    writer.Write("d");
                    foreach (string k in dict.Keys.OfType<string>().OrderBy(x => x, KeyComparer.Instance))
                    {
                        var value = dict[k];
                        if (value != null)
                        {
                            Encode(writer, k);
                            Encode(writer, value);
                        }
                    }
                    writer.Write("e");
                    break;
                default:
                    throw new FormatException();
            }

        }
        class KeyComparer : IComparer<string>
        {
            public static KeyComparer Instance = new KeyComparer();

            public int Compare(string? x, string? y)
            {
                var lhs = Encoding.GetBytes(x!);
                var rhs = Encoding.GetBytes(y!);
                for (int i = 0, j = 0; i < lhs.Length && j < rhs.Length; i++, j++)
                {
                    if (lhs[i] != rhs[j])
                        return lhs[i] - rhs[j];
                }
                return lhs.Length - rhs.Length;
            }
        }
    }
}
