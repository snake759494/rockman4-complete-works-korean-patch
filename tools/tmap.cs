using System; using System.IO; using System.Drawing; using System.Collections.Generic;
// tmap <shot> <02.bin> : map screen tiles (x 80..200, y 64..96) to page4 grid cells
class TMap { static void Main(string[] a) {
    var im = new Bitmap(a[0]); var d = File.ReadAllBytes(a[1]); var t = new Tim(d, 0x2012C); int W = int.Parse(a[2]);
    int GX = int.Parse(a[3]); for (int sy = 64; sy < 100; sy += 8) for (int sx = 80 + GX; sx < 208; sx += 8) {
        var m = new bool[8, 8]; int on = 0;
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) { var c = im.GetPixel(sx + x, sy + y); m[x, y] = c.R > 180 && c.G > 180 && c.B > 180; if (m[x, y]) on++; }
        var hits = new List<string>();
        for (int py = 4; py + 8 <= t.H; py += 8) for (int px = 0; px + 8 <= t.W; px += 1) {
            bool ok = true; for (int y = 0; y < 8 && ok; y++) for (int x = 0; x < 8; x++) if ((t.Get(px + x, py + y) == W) != m[x, y]) { ok = false; break; }
            if (ok) hits.Add(px + "," + py);
        }
        Console.WriteLine("{0},{1} on={2} -> {3}", sx, sy, on, hits.Count > 4 ? hits.Count + " hits" : string.Join(" ", hits));
    }
} }
