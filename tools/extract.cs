using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

// extract byte-string blocks into TSV: id \t file \t off \t maxbytes \t text
// text: 8-cell lines joined by "/", pages by " || ", FE shown as "_"
class Extract {
    static string Dec(byte[] d, int s, int e, int lineW) {
        var sb = new StringBuilder(); int cell = 0;
        for (int i = s; i < e; i++) {
            int b = d[i];
            if (b == 0xFF) { sb.Append(" || "); cell = 0; continue; }
            if (cell > 0 && cell % lineW == 0) sb.Append("/");
            sb.Append(Sysf.Ch(b)); cell++;
        }
        return sb.ToString();
    }
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        string file = a[0]; int s = Convert.ToInt32(a[1], 16), e = Convert.ToInt32(a[2], 16);
        string pre = a[3]; int lineW = int.Parse(a[4]);
        byte[] d = File.ReadAllBytes(file);
        var o = new StreamWriter(a[5], false, new UTF8Encoding(false));
        int id = 0, i = s;
        while (i < e) {
            while (i < e && d[i] == 0) i++;
            if (i >= e) break;
            int st = i;
            while (i < e && d[i] != 0) i++;
            int en = i;
            int z = i; while (z < e && d[z] == 0) z++;
            int max = z - st - 1; // keep at least one 0 terminator
            o.WriteLine("{0}{1:D3}\t{2}\t{3:X}\t{4}\t{5}\t", pre, id++, Path.GetFileName(file), st, max, Dec(d, st, en, lineW));
            i = z;
        }
        o.Close();
    }
}
