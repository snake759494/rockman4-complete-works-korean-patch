using System; using System.IO; using System.Drawing; using System.Drawing.Imaging;
// rawview <file> <off hex> <width px> <bpp 16|24|8> <out.png>
class RawView { static void Main(string[] a) {
    var d = File.ReadAllBytes(a[0]); int off = Convert.ToInt32(a[1], 16), w = int.Parse(a[2]), bpp = int.Parse(a[3]);
    int bpr = w * bpp / 8; int h = (d.Length - off) / bpr; if (h > 600) h = 600;
    using (var b = new Bitmap(w, h)) {
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
            int o = off + y * bpr + x * bpp / 8; Color c;
            if (bpp == 16) { int v = d[o] | d[o + 1] << 8; c = Color.FromArgb((v & 31) * 8, ((v >> 5) & 31) * 8, ((v >> 10) & 31) * 8); }
            else if (bpp == 24) c = Color.FromArgb(d[o], d[o + 1], d[o + 2]);
            else c = Color.FromArgb(d[o], d[o], d[o]);
            b.SetPixel(x, y, c);
        }
        b.Save(a[4], ImageFormat.Png);
    }
} }
