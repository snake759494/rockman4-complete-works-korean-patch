using System;
using System.IO;
using System.Text;

// hw <file> : find halfword glyph arrays terminated by 0xFFFF
class Hw {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        byte[] d = File.ReadAllBytes(a[0]);
        int minLen = a.Length > 1 ? int.Parse(a[1]) : 3;
        for (int o = 0; o + 2 <= d.Length; o += 2) {
            int st = o, n = 0, kana = 0;
            while (o + 2 <= d.Length) {
                int v = BitConverter.ToUInt16(d, o);
                if (v == 0xFFFF) break;
                if (v > 0xFF) { n = -1; break; }
                if (v >= 0x10 && v <= 0xFC) kana++;
                n++; o += 2;
            }
            if (n >= minLen && o + 2 <= d.Length && BitConverter.ToUInt16(d, o) == 0xFFFF && kana * 2 >= n) {
                var sb = new StringBuilder();
                for (int i = st; i < o; i += 2) {
                    int v = BitConverter.ToUInt16(d, i);
                    sb.Append(v < Sysf.G.Length ? Sysf.G[v].ToString() : (v == 0xFD ? "_" : "{" + v.ToString("X2") + "}"));
                }
                Console.WriteLine("{0:X6} [{1}] {2}", st, n, sb);
            }
            if (n < 0) o = st;
        }
    }
}
