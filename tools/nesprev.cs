using System; using System.IO; using System.Drawing; using System.Drawing.Imaging; using System.Collections.Generic;
// nesprev <22.bin> <02.bin> <start> <end> <out.png> : render NES text using patched font tiles
class NesPrev { static void Main(string[] a) {
    var rom = File.ReadAllBytes(a[0]); var t = new Tim(File.ReadAllBytes(a[1]), 0x04022C);
    int s = Convert.ToInt32(a[2], 16), e = Convert.ToInt32(a[3], 16);
    var lines = new List<List<int>>(); var cur = new List<int>(); lines.Add(cur);
    for (int i = s; i < e; i++) { int b = rom[i];
        if (b == 0xFD) { cur = new List<int>(); lines.Add(cur); }
        else if (b == 0xFE) { lines.Add(new List<int>()); cur = new List<int>(); lines.Add(cur); }
        else if (b == 0xFF) { lines.Add(new List<int> { -1 }); cur = new List<int>(); lines.Add(cur); }
        else if (b == 0x0A) { }
        else cur.Add(b); }
    int S = 3; using (var bmp = new Bitmap(28 * 8 * S, lines.Count * 10 * S)) using (var g = Graphics.FromImage(bmp)) {
        g.Clear(Color.Black);
        for (int li = 0; li < lines.Count; li++) for (int ci = 0; ci < lines[li].Count; ci++) { int c = lines[li][ci]; if (c < 0) continue;
            int n = c <= 0x56 ? c + 29 : c + 28; int cx = 4 + 8 * (n % 31), cy = 36 + 8 * (n / 31);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (c != 0 && t.Get(cx + x, cy + y) != 0) g.FillRectangle(Brushes.White, (ci * 8 + x) * S, (li * 10 + y) * S, S, S); }
        bmp.Save(a[4], ImageFormat.Png); }
} }
