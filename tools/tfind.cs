using System; using System.IO; using System.Drawing; using System.Collections.Generic;
// tfind <shot> <02.bin> <sx> <sy> : find 8x8 white pattern anywhere in all pages, any index
class TFind { static void Main(string[] a) {
    var im = new Bitmap(a[0]); var d = File.ReadAllBytes(a[1]); int sx = int.Parse(a[2]), sy = int.Parse(a[3]);
    var m = new bool[8, 8]; for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) { var c = im.GetPixel(sx + x, sy + y); m[x, y] = c.R > 180 && c.G > 180 && c.B > 180; }
    int[] offs = Array.ConvertAll(a[4].Split(new[]{','}), s => Convert.ToInt32(s, 16));
    for (int p = 0; p < offs.Length; p++) { var t = new Tim(d, offs[p]);
        for (int py = 0; py + 8 <= t.H; py++) for (int px = 0; px + 8 <= t.W; px++) for (int W = 1; W <= 3; W++) for (int fl = 0; fl < 2; fl++) {
            int mis = 0; int onp = 0;
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) { int xx = fl == 1 ? 7 - x : x; bool pv = t.Get(px + xx, py + y) == W; if (pv) onp++; if (pv != m[x, y]) mis++; } if (mis > 2) break;
            if (mis <= 2 && onp >= 8) Console.WriteLine("page{0} ({1},{2}) W={3} flip={4} mis={5}", p, px, py, W, fl, mis);
        } }
} }
