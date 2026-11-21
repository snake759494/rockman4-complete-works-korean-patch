using System;
using System.IO;
using System.Text;

// dbx <28.bin> <out.tsv> : records of 0xC4 from 0x24; name at +0 (max 12 hw), desc at +0x1A
class Dbx {
    static string Dec(byte[] d, int s, out int end) {
        var sb = new StringBuilder(); int i = s;
        while (i + 1 < d.Length) {
            int v = BitConverter.ToUInt16(d, i);
            if (v == 0xFFFF) break;
            if (v == 0xFFFE) sb.Append("^");
            else if (v == 0xFD) sb.Append("_");
            else if (v < Sysf.G.Length) sb.Append(Sysf.G[v]);
            else sb.Append("{" + v.ToString("X") + "}");
            i += 2;
        }
        end = i; return sb.ToString();
    }
    static void Main(string[] a) {
        byte[] d = File.ReadAllBytes(a[0]);
        var o = new StreamWriter(a[1], false, new UTF8Encoding(false));
        for (int k = 0; ; k++) {
            int b = 0x24 + k * 0xC4;
            if (b + 0x1A >= d.Length) break;
            int e1; string name = Dec(d, b, out e1);
            int nmax = 12;
            int ds = b + 0x1A, e2; string desc = Dec(d, ds, out e2);
            int z = e2 + 2; while (z + 1 < d.Length && z < b + 0xC4 && BitConverter.ToUInt16(d, z) == 0) z += 2;
            int dmax = (z - ds) / 2 - 1;
            o.WriteLine("D{0:D2}N\t28.bin\t{1:X}\t{2}\t0\t{3}\t", k, b, nmax, name);
            if (desc.Length > 0) o.WriteLine("D{0:D2}T\t28.bin\t{1:X}\t{2}\t0\t{3}\t", k, ds, dmax, desc);
        }
        o.Close();
    }
}
