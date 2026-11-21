using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

// tim scan <file> <outdir> : find TIM images and dump PNG (clut 0)
class Tim {
    static int U16(byte[] d, int o) { return d[o] | d[o+1] << 8; }
    static int U32(byte[] d, int o) { return d[o] | d[o+1] << 8 | d[o+2] << 16 | d[o+3] << 24; }
    static Color C15(int v) {
        int r = (v & 31) * 255 / 31, g = ((v >> 5) & 31) * 255 / 31, b = ((v >> 10) & 31) * 255 / 31;
        int a = (v == 0) ? 0 : 255;
        return Color.FromArgb(a, r, g, b);
    }
    public static int TryParse(byte[] d, int o, string outPng, int clutIdx) {
        if (o + 8 > d.Length || U32(d, o) != 0x10) return 0;
        int fl = U32(d, o + 4); if ((fl & ~0xB) != 0) return 0;
        int bpp = fl & 3; bool hasClut = (fl & 8) != 0;
        int p = o + 8;
        int[] clut = null; int cw = 0, ch = 0;
        if (hasClut) {
            if (p + 12 > d.Length) return 0;
            int len = U32(d, p); cw = U16(d, p + 8); ch = U16(d, p + 10);
            if (len != 12 + cw * ch * 2 || cw == 0 || ch == 0 || cw > 256 || ch > 512) return 0;
            clut = new int[cw * ch];
            for (int i = 0; i < cw * ch; i++) clut[i] = U16(d, p + 12 + i * 2);
            p += len;
        } else if (bpp < 2) return 0;
        if (p + 12 > d.Length) return 0;
        int ilen = U32(d, p); int fx = U16(d, p+4), fy = U16(d, p+6); int w16 = U16(d, p + 8), h = U16(d, p + 10);
        if (ilen != 12 + w16 * h * 2 || w16 == 0 || h == 0 || w16 > 1024 || h > 512) return 0;
        if (p + ilen > d.Length) return 0;
        int w = bpp == 0 ? w16 * 4 : bpp == 1 ? w16 * 2 : bpp == 2 ? w16 : w16 * 2 / 3;
        if (outPng != null) {
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb)) {
                int b = p + 12;
                int cb = clut != null ? Math.Min(clutIdx, ch - 1) * cw : 0;
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
                    int v;
                    if (bpp == 0) { int byt = d[b + y * w16 * 2 + x / 2]; v = clut[cb + ((x & 1) == 0 ? byt & 15 : byt >> 4)]; }
                    else if (bpp == 1) { v = clut[cb + d[b + y * w16 * 2 + x]]; }
                    else v = U16(d, b + y * w16 * 2 + x * 2);
                    bmp.SetPixel(x, y, C15(v));
                }
                bmp.Save(outPng, ImageFormat.Png);
            }
            Console.WriteLine("{0:X6} bpp{1} {2}x{3} fb({4},{5}) clut {6}x{7} len {8:X}", o, bpp, w, h, fx, fy, cw, ch, p + ilen - o);
        }
        return p + ilen - o;
    }
    static void Main(string[] a) {
        byte[] d = File.ReadAllBytes(a[1]);
        Directory.CreateDirectory(a[2]);
        string bn = Path.GetFileNameWithoutExtension(a[1]);
        for (int o = 0; o + 8 < d.Length; o += 4) {
            int n = TryParse(d, o, null, 0);
            if (n > 0) { TryParse(d, o, Path.Combine(a[2], bn + "_" + o.ToString("X6") + ".png"), 0); o += n - 4; }
        }
    }
}
