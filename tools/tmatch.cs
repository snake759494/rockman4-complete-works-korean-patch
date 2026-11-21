using System; using System.IO; using System.Drawing; using System.Collections.Generic;
class TMatch { static void Main(string[] a) {
    var im = new Bitmap(a[0]); double sx = 1.0;
    int X0 = 88, Y0 = 72, W = 110, H = 24;
    var m = new bool[W, H];
    for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { var c = im.GetPixel((int)((X0 + x + 0.5) * sx), Y0 + y); m[x, y] = c.R > 180 && c.G > 180 && c.B > 180; }
    var d = File.ReadAllBytes(a[1]);
    var pages = new List<Tim>(); int[] offs = { 0x2C, 0x806C, 0x100AC, 0x180EC, 0x2012C, 0x2816C, 0x301AC, 0x381EC, 0x4022C, 0x4826C };
    foreach (var o in offs) pages.Add(new Tim(d, o));
    for (int oy = 0; oy < 8; oy++) for (int ox = 0; ox < 8; ox++) {
        int hits = 0; var res = new List<string>();
        for (int ty = oy; ty + 8 <= H; ty += 8) for (int tx = ox; tx + 8 <= W; tx += 8) {
            int on = 0; for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (m[tx + x, ty + y]) on++;
            if (on < 10) continue;
            for (int p = 0; p < pages.Count; p++) { var t = pages[p];
                for (int py = 0; py + 8 <= t.H; py++) for (int px = 0; px + 8 <= t.W; px++) {
                    bool ok = true;
                    for (int y = 0; y < 8 && ok; y++) for (int x = 0; x < 8; x++) if ((t.Get(px + x, py + y) != 0) != m[tx + x, ty + y]) { ok = false; break; }
                    if (ok) { hits++; res.Add(string.Format("scr({0},{1}) page{2} ({3},{4})", X0 + tx, Y0 + ty, p, px, py)); }
                } }
        }
        if (hits > 0) { Console.WriteLine("grid {0},{1}: {2}", ox, oy, hits); foreach (var r in res) Console.WriteLine("  " + r); }
    }
} }
