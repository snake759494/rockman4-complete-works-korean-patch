using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

// hwx <file> <start> <end> <bias> <prefix> <out.tsv> [append]
// bias0: strings end at FFFF, FFFE = newline "^"; bias1: strings end at 0000
class Hwx {
    static bool IsGlyph(int v, int bias) {
        if (bias == 0) return v == 0xFFFE || (v >= 0 && v <= 0xFD);
        return v >= 1 && v <= 0xFE;
    }
    static string Ch(int v, int bias) {
        if (bias == 0 && v == 0xFFFE) return "^";
        int g = v - bias;
        if (g == 0xFD) return "_";
        return Sysf.G[g].ToString();
    }
    static void Main(string[] a) {
        byte[] d = File.ReadAllBytes(a[0]);
        int s = Convert.ToInt32(a[1], 16), e = Convert.ToInt32(a[2], 16), bias = int.Parse(a[3]);
        string pre = a[4];
        var o = new StreamWriter(a[5], a.Length > 6, new UTF8Encoding(false));
        int term = bias == 0 ? 0xFFFF : 0;
        int id = 0;
        int i = s;
        while (i + 1 < e) {
            int v = BitConverter.ToUInt16(d, i);
            // skip terminators / padding
            if (v == term || (bias == 0 && v == 0 && (i & 3) == 2 && i >= 2 && BitConverter.ToUInt16(d, i - 2) == 0xFFFF)) { i += 2; continue; }
            int st = i; bool ok = true; var sb = new StringBuilder();
            while (i + 1 < e) {
                v = BitConverter.ToUInt16(d, i);
                if (v == term) break;
                if (!IsGlyph(v, bias)) { ok = false; break; }
                sb.Append(Ch(v, bias)); i += 2;
            }
            if (!ok) { i += 2; continue; }
            int en = i; // terminator position
            int z = i + 2; // count extra padding after terminator
            if (bias == 1) while (z + 1 < e && BitConverter.ToUInt16(d, z) == 0) z += 2;
            else { if (z + 1 < e && (z & 3) == 2 && BitConverter.ToUInt16(d, z) == 0) z += 2; }
            int maxHw = (z - st) / 2 - 1; // reserve 1 terminator
            if (sb.Length >= 2) o.WriteLine("{0}{1:D3}\t{2}\t{3:X}\t{4}\t{5}\t{6}\t", pre, id++, Path.GetFileName(a[0]), st, maxHw, bias, sb);
            i = en + 2;
        }
        o.Close();
    }
}
