using System;
using System.IO;
using System.Text;

// sj <file> [minchars] : dump Shift-JIS runs
class Sj {
    static void Main(string[] a) {
        byte[] d = File.ReadAllBytes(a[0]);
        int min = a.Length > 1 ? int.Parse(a[1]) : 4;
        var enc = Encoding.GetEncoding(932);
        var outw = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
        int i = 0;
        while (i < d.Length) {
            int st = i, cnt = 0;
            while (i + 1 < d.Length) {
                int b = d[i];
                if ((b >= 0x81 && b <= 0x9F) || (b >= 0xE0 && b <= 0xEA)) {
                    int c = d[i+1];
                    if (c >= 0x40 && c <= 0xFC && c != 0x7F) { i += 2; cnt++; continue; }
                }
                if (b >= 0x20 && b < 0x7F && cnt > 0) { i++; continue; }
                break;
            }
            if (cnt >= min) outw.WriteLine("{0:X6}: {1}", st, enc.GetString(d, st, i - st));
            if (i == st) i++;
        }
        outw.Flush();
    }
}
