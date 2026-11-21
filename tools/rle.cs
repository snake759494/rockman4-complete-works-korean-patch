using System;
using System.IO;
using System.Collections.Generic;

// RLE type-2 codec used by DATA.PAC entry 0
public static class Rle {
    public static byte[] Decode(byte[] s, out List<byte[]> hdrs, out List<int> blockOffs) {
        int type = BitConverter.ToUInt16(s, 0), n = s[2]; int size = BitConverter.ToInt32(s, 4);
        var o = new byte[size];
        hdrs = new List<byte[]>(); blockOffs = new List<int>();
        int tab = 8; int p = 8 + (n - 1) * 4;
        for (int b = 0; b < n; b++) {
            var h = new byte[4]; Array.Copy(s, p, h, 0, 4); p += 4; hdrs.Add(h);
            int dst = b == 0 ? 0 : BitConverter.ToInt32(s, tab + (b - 1) * 4);
            blockOffs.Add(dst);
            int stride = h[0] + 1;
            for (int c = 0; c < stride; c++) {
                int v = dst + c;
                while (s[p] != h[3]) {
                    int a = s[p];
                    if (a == h[1]) { int cnt = s[p + 1], val = s[p + 2]; p += 3; for (int k = 0; k < cnt; k++) { o[v] = (byte)val; v += stride; } }
                    else if (a == h[2]) { int val = s[p + 1]; p += 2; for (int k = 0; k < 3; k++) { o[v] = (byte)val; v += stride; } }
                    else { o[v] = (byte)a; v += stride; p++; }
                }
                p++; // end marker
            }
        }
        return o;
    }
    public static byte[] Encode(byte[] raw, byte[] origHeader, List<byte[]> hdrs, List<int> blockOffs) {
        var outp = new List<byte>();
        int n = hdrs.Count;
        outp.AddRange(new byte[] { origHeader[0], origHeader[1], (byte)n, origHeader[3] });
        outp.AddRange(BitConverter.GetBytes(raw.Length));
        for (int b = 1; b < n; b++) outp.AddRange(BitConverter.GetBytes(blockOffs[b]));
        for (int b = 0; b < n; b++) {
            var h = hdrs[b]; int stride = h[0] + 1;
            int start = blockOffs[b], end = b + 1 < n ? blockOffs[b + 1] : raw.Length;
            outp.AddRange(h);
            for (int c = 0; c < stride; c++) {
                var seq = new List<byte>();
                for (int i = start + c; i < end; i += stride) seq.Add(raw[i]);
                int k = 0;
                while (k < seq.Count) {
                    byte v = seq[k]; int run = 1;
                    while (k + run < seq.Count && seq[k + run] == v && run < 255) run++;
                    bool esc = v == h[1] || v == h[2] || v == h[3];
                    if (run >= 4 || (esc && run != 3)) { outp.Add(h[1]); outp.Add((byte)run); outp.Add(v); k += run; }
                    else if (run == 3) { outp.Add(h[2]); outp.Add(v); k += 3; }
                    else { for (int r = 0; r < run; r++) outp.Add(v); k += run; }
                }
                outp.Add(h[3]);
            }
        }
        outp.Add(hdrs[n - 1][3]); // final terminator (checked by loop end)
        return outp.ToArray();
    }
}
