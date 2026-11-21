using System; using System.IO; using System.Drawing; using System.Drawing.Imaging;
// chr <file> <off hex> <ntiles> <out.png> : render NES 2bpp CHR, 32 tiles per row, scale 2, with hex index every 16 tiles row label
class Chr { static void Main(string[] a) {
    var d = File.ReadAllBytes(a[0]); int off = Convert.ToInt32(a[1], 16), n = int.Parse(a[2]);
    int rows = (n + 31) / 32; int S = 2;
    using (var b = new Bitmap(32 * 8 * S, rows * 8 * S)) {
        Color[] pal = { Color.Black, Color.Red, Color.Lime, Color.White };
        for (int t = 0; t < n; t++) { int o = off + t * 16; if (o + 16 > d.Length) break;
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) {
                int v = ((d[o + y] >> (7 - x)) & 1) | (((d[o + 8 + y] >> (7 - x)) & 1) << 1);
                for (int k = 0; k < S * S; k++) b.SetPixel(((t % 32) * 8 + x) * S + k % S, ((t / 32) * 8 + y) * S + k / S, pal[v]);
            } }
        b.Save(a[3], ImageFormat.Png);
    }
} }
