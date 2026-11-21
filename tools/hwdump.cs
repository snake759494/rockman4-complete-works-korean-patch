using System;
using System.IO;
using System.Text;

// hwdump <file> <start> <end> <bias> : decode halfword runs (value-bias = glyph), split at FFFF
class HwDump {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        byte[] d = File.ReadAllBytes(a[0]);
        int s = Convert.ToInt32(a[1], 16), e = Convert.ToInt32(a[2], 16), bias = int.Parse(a[3]);
        var sb = new StringBuilder(); int st = s;
        for (int i = s; i + 1 < e; i += 2) {
            int v = BitConverter.ToUInt16(d, i);
            if (v == 0xFFFF) { Console.WriteLine("{0:X6}-{1:X6}: {2}", st, i, sb); sb.Clear(); st = i + 2; continue; }
            int g = v - bias;
            if (v == 0 && bias == 1) sb.Append("|");
            else if (g >= 0 && g < Sysf.G.Length) sb.Append(g == 0xFD ? '_' : Sysf.G[g]);
            else sb.Append("{" + v.ToString("X") + "}");
        }
        if (sb.Length > 0) Console.WriteLine("{0:X6}-{1:X6}: {2}", st, e, sb);
    }
}
