using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

public class Tim {
    public byte[] D; public int Off, Bpp, W, H, Pix, ClutOff, ClutW, ClutH, Stride;
    public Tim(byte[] d, int off) {
        D = d; Off = off;
        int fl = BitConverter.ToInt32(d, off + 4); Bpp = fl & 3;
        int p = off + 8;
        if ((fl & 8) != 0) { ClutOff = p + 12; ClutW = BitConverter.ToUInt16(d, p + 8); ClutH = BitConverter.ToUInt16(d, p + 10); p += BitConverter.ToInt32(d, p); }
        int w16 = BitConverter.ToUInt16(d, p + 8); H = BitConverter.ToUInt16(d, p + 10);
        Stride = w16 * 2; Pix = p + 12;
        W = Bpp == 0 ? w16 * 4 : Bpp == 1 ? w16 * 2 : w16;
    }
    public int Get(int x, int y) {
        if (Bpp == 0) { int b = D[Pix + y * Stride + x / 2]; return (x & 1) == 0 ? b & 15 : b >> 4; }
        if (Bpp == 1) return D[Pix + y * Stride + x];
        return BitConverter.ToUInt16(D, Pix + y * Stride + x * 2);
    }
    public void Set(int x, int y, int v) {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        if (Bpp == 0) { int o = Pix + y * Stride + x / 2; D[o] = (byte)((x & 1) == 0 ? (D[o] & 0xF0) | v : (D[o] & 0x0F) | (v << 4)); }
        else if (Bpp == 1) D[Pix + y * Stride + x] = (byte)v;
        else { D[Pix + y * Stride + x * 2] = (byte)v; D[Pix + y * Stride + x * 2 + 1] = (byte)(v >> 8); }
    }
    public Color Col(int v, int clutRow = 0) {
        int c = Bpp == 2 ? v : BitConverter.ToUInt16(D, ClutOff + (clutRow * ClutW + v) * 2);
        if (c == 0) return Color.FromArgb(255, 60, 0, 60);
        return Color.FromArgb(255, (c & 31) * 255 / 31, ((c >> 5) & 31) * 255 / 31, ((c >> 10) & 31) * 255 / 31);
    }
    public Bitmap ToBitmap(int scale) {
        var b = new Bitmap(W * scale, H * scale);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { var c = Col(Get(x, y)); for (int k = 0; k < scale * scale; k++) b.SetPixel(x * scale + k % scale, y * scale + k / scale, c); }
        return b;
    }
}

public class Bdf {
    public class G { public int W, H, Xo, Yo, Dw; public uint[] Rows; }
    public Dictionary<int, G> Glyphs = new Dictionary<int, G>();
    public int Ascent;
    public Bdf(string path) {
        G cur = null; int enc = -1; bool bmp = false; var rows = new List<uint>();
        foreach (var raw in File.ReadLines(path)) {
            var l = raw.Trim();
            if (l.StartsWith("FONT_ASCENT")) Ascent = int.Parse(l.Split(' ')[1]);
            else if (l.StartsWith("ENCODING")) enc = int.Parse(l.Split(' ')[1]);
            else if (l.StartsWith("STARTCHAR")) { cur = new G(); rows.Clear(); bmp = false; }
            else if (l.StartsWith("DWIDTH")) cur.Dw = int.Parse(l.Split(' ')[1]);
            else if (l.StartsWith("BBX")) { var p = l.Split(' '); cur.W = int.Parse(p[1]); cur.H = int.Parse(p[2]); cur.Xo = int.Parse(p[3]); cur.Yo = int.Parse(p[4]); }
            else if (l == "BITMAP") bmp = true;
            else if (l == "ENDCHAR") { cur.Rows = rows.ToArray(); Glyphs[enc] = cur; bmp = false; }
            else if (bmp) rows.Add(Convert.ToUInt32(l.PadRight(8, '0').Substring(0, 8), 16));
        }
    }
    public bool Px(G g, int x, int y) { if (x < 0 || y < 0 || x >= g.W || y >= g.H) return false; return (g.Rows[y] >> (31 - x) & 1) != 0; }
    // render text to mask with baseline at row 'baseline'
    public bool[,] Line(string s, int height, int baseline) {
        int w = 0; foreach (char c in s) { G g; w += Glyphs.TryGetValue(c, out g) ? g.Dw : 4; }
        var m = new bool[Math.Max(1, w), height]; int cx = 0;
        foreach (char c in s) {
            G g; if (!Glyphs.TryGetValue(c, out g)) { cx += 4; continue; }
            int top = baseline + 1 - g.Yo - g.H;
            for (int y = 0; y < g.H; y++) for (int x = 0; x < g.W; x++) if (Px(g, x, y)) { int px = cx + g.Xo + x, py = top + y; if (px >= 0 && py >= 0 && px < w && py < height) m[px, py] = true; }
            cx += g.Dw;
        }
        return m;
    }
}

public static class Extra {
    static Dictionary<string, Bdf> Bdfs = new Dictionary<string, Bdf>();
    public static Bdf GetBdf(string key) { Bdf b; if (!Bdfs.TryGetValue(key, out b)) { b = new Bdf(Build.Work + @"\fonts\Galmuri" + key.Substring(1) + ".bdf"); Bdfs[key] = b; } return b; }
    static Dictionary<string, FontFamily> Fams = new Dictionary<string, FontFamily>();
    static List<PrivateFontCollection> keep = new List<PrivateFontCollection>();
    public static FontFamily Fam(string key) {
        FontFamily f; if (Fams.TryGetValue(key, out f)) return f;
        string file = Build.Root + @"\NanumSquareNeo-" + key.Substring(4) + ".ttf";
        var pfc = new PrivateFontCollection(); pfc.AddFontFile(file); keep.Add(pfc);
        f = pfc.Families[0]; Fams[key] = f; return f;
    }

    // render one line to a bool mask; returns width/height via mask dims. pixel fonts (G*) rendered 1:1.
    public static bool[,] RenderLine(string text, string font, float size, float sx) {
        bool pixel = font.StartsWith("G");
        if (pixel && (font == "G7" || font == "G9")) {
            var bdf = GetBdf(font);
            var mm = bdf.Line(text, 16, 10);
            var tt = Trim(mm);
            if (Math.Abs(sx - 1f) > 0.01f) tt = ScaleX(tt, sx);
            return tt;
        }
        if (pixel) {
            var fam = Fam(font);
            int w = (int)(text.Length * size * 1.5f) + 8, h = (int)(size * 2) + 4;
            using (var b = new Bitmap(w, h)) using (var g = Graphics.FromImage(b)) {
                g.Clear(Color.Black); g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                using (var f = new Font(fam, size, FontStyle.Regular, GraphicsUnit.Pixel)) {
                    var fmt = StringFormat.GenericTypographic; fmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                    g.DrawString(text, f, Brushes.White, 0, 0, fmt);
                }
                var m = new bool[w, h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) m[x, y] = b.GetPixel(x, y).R > 128;
                var t = Trim(m);
                if (Math.Abs(sx - 1f) > 0.01f) t = ScaleX(t, sx);
                return t;
            }
        }
        int S = 6;
        var famn = Fam(font);
        using (var path = new GraphicsPath()) {
            path.AddString(text, famn, (int)FontStyle.Regular, size * S, new PointF(0, 0), StringFormat.GenericTypographic);
            var bb = path.GetBounds();
            // vertical reference: hangul '한' bounds so baseline consistent
            RectangleF rb; using (var rp = new GraphicsPath()) { rp.AddString("한", famn, 0, size * S, new PointF(0, 0), StringFormat.GenericTypographic); rb = rp.GetBounds(); }
            float top = Math.Min(bb.Y, rb.Y), bot = Math.Max(bb.Bottom, rb.Bottom);
            int W = (int)Math.Ceiling((bb.Width * sx) / S) + 2, H = (int)Math.Ceiling((bot - top) / S) + 2;
            using (var m = new Matrix()) { m.Translate(-bb.X * sx + S, -top + S); m.Scale(sx, 1, MatrixOrder.Prepend); path.Transform(new Matrix(sx, 0, 0, 1, -bb.X * sx + S, -top + S)); }
            using (var b = new Bitmap(W * S, H * S, PixelFormat.Format32bppArgb)) using (var g = Graphics.FromImage(b)) {
                g.Clear(Color.Black); g.SmoothingMode = SmoothingMode.AntiAlias; g.FillPath(Brushes.White, path);
                var data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var buf = new byte[data.Stride * b.Height]; System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length); b.UnlockBits(data);
                var mk = new bool[W, H];
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
                    int s = 0; for (int yy = 0; yy < S; yy++) for (int xx = 0; xx < S; xx++) s += buf[(y * S + yy) * data.Stride + (x * S + xx) * 4 + 1];
                    mk[x, y] = s >= S * S * 255 * 0.45;
                }
                // keep vertical reference: trim only horizontally
                return TrimX(mk);
            }
        }
    }
    static bool[,] Trim(bool[,] m) {
        int w = m.GetLength(0), h = m.GetLength(1), x0 = w, x1 = -1, y0 = h, y1 = -1;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (m[x, y]) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        if (x1 < 0) return new bool[1, 1];
        var r = new bool[x1 - x0 + 1, y1 - y0 + 1];
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) r[x - x0, y - y0] = m[x, y];
        return r;
    }
    static bool[,] TrimX(bool[,] m) {
        int w = m.GetLength(0), h = m.GetLength(1), x0 = w, x1 = -1, y0 = h, y1 = -1;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (m[x, y]) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        if (x1 < 0) return new bool[1, 1];
        var r = new bool[x1 - x0 + 1, y1 - y0 + 1];
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) r[x - x0, y - y0] = m[x, y];
        return r;
    }
    static bool[,] ScaleX(bool[,] m, float sx) {
        int w = m.GetLength(0), h = m.GetLength(1), nw = Math.Max(1, (int)Math.Round(w * sx));
        var r = new bool[nw, h];
        for (int y = 0; y < h; y++) for (int x = 0; x < nw; x++) r[x, y] = m[Math.Min(w - 1, (int)(x / sx)), y];
        return r;
    }

    public class Op {
        public string File, Kind, Text; public int TimOff;
        public Dictionary<string, string> P = new Dictionary<string, string>();
        public string S(string k, string d) { string v; return P.TryGetValue(k, out v) ? v : d; }
        public int I(string k, int d) { string v; return P.TryGetValue(k, out v) ? int.Parse(v) : d; }
        public float Fl(string k, float d) { string v; return P.TryGetValue(k, out v) ? float.Parse(v, System.Globalization.CultureInfo.InvariantCulture) : d; }
    }

    static List<Bitmap> previews = new List<Bitmap>();
    static List<string> previewNames = new List<string>();

    public static void Label(Op op) {
        var t = new Tim(Build.F(op.File), op.TimOff);
        var before = t.ToBitmap(2);
        int rx = 0, ry = 0, rw = t.W, rh = t.H;
        if (op.P.ContainsKey("rect")) { var r = op.P["rect"].Split(','); rx = int.Parse(r[0]); ry = int.Parse(r[1]); rw = int.Parse(r[2]); rh = int.Parse(r[3]); }
        int bgx = op.I("bgx", -1);
        int bgAuto;
        {
            var cnt = new Dictionary<int, int>();
            for (int x = 0; x < rw; x++) foreach (int yy in new[] { ry, ry + rh - 1 }) { int v = t.Get(rx + x, yy); int c; cnt.TryGetValue(v, out c); cnt[v] = c + 1; }
            for (int y = 0; y < rh; y++) foreach (int xx in new[] { rx, rx + rw - 1 }) { int v = t.Get(xx, ry + y); int c; cnt.TryGetValue(v, out c); cnt[v] = c + 1; }
            bgAuto = cnt.OrderByDescending(k => k.Value).First().Key;
        }
        int bgFixed = op.I("bg", bgAuto); if (op.P.ContainsKey("bgrgb")) { var pp = op.P["bgrgb"].Split(',').Select(int.Parse).ToArray(); bgFixed = NearestIdx(t, Color.FromArgb(pp[0], pp[1], pp[2])); }
        var keep = new HashSet<int>(op.S("keep", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
        Func<int, int> bg0 = y => bgx >= 0 ? t.Get(bgx, y) : bgFixed;
        Func<int, int> bg = bg0;
        if (op.S("bgmode", "") == "row") {
            var rowBg = new int[t.H];
            for (int y = ry; y < ry + rh; y++) {
                var c = new Dictionary<int, int>();
                for (int x = rx; x < rx + rw; x++) {
                    int v = t.Get(x, y);
                    var col = t.Col(v); double lum = (col.R * 0.3 + col.G * 0.59 + col.B * 0.11) / 255.0;
                    if (lum > 0.80 || lum < 0.10) continue; // skip text-like (white / black) colours
                    int cn; c.TryGetValue(v, out cn); c[v] = cn + 1;
                }
                rowBg[y] = c.Count > 0 ? c.OrderByDescending(k => k.Value).First().Key : (y > ry ? rowBg[y - 1] : t.Get(rx, y));
            }
            bg = y => rowBg[y];
        }
        Func<string, int> nearest = rgb => {
            var p = rgb.Split(',').Select(int.Parse).ToArray();
            int best = 0; double bd = 1e9; int nn = t.Bpp == 0 ? 16 : t.Bpp == 1 ? 256 : 0;
            if (nn == 0) return 0x8000 | (p[0] * 31 / 255) | ((p[1] * 31 / 255) << 5) | ((p[2] * 31 / 255) << 10);
            for (int i = 0; i < Math.Min(nn, t.ClutW); i++) {
                int c = BitConverter.ToUInt16(t.D, t.ClutOff + i * 2); if (c == 0) continue;
                double dr = (c & 31) * 255 / 31 - p[0], dg = ((c >> 5) & 31) * 255 / 31 - p[1], db = ((c >> 10) & 31) * 255 / 31 - p[2];
                double dd = dr * dr + dg * dg + db * db; if (dd < bd) { bd = dd; best = i; }
            }
            return best;
        };
        if (op.P.ContainsKey("fillrgb")) op.P["fill"] = nearest(op.P["fillrgb"]).ToString();
        if (op.P.ContainsKey("olrgb")) op.P["olc"] = nearest(op.P["olrgb"]).ToString();
        // distance map of original text pixels within region
        var dist = new int[rw, rh];
        for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) { int v = t.Get(rx + x, ry + y); dist[x, y] = (v == bg(ry + y) || keep.Contains(v)) ? 0 : 99; }
        for (int it = 1; it < 6; it++)
            for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) {
                if (dist[x, y] != 99) continue;
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= rw || yy >= rh) continue;
                    if (dist[xx, yy] == it - 1) { near = true; break; }
                }
                if (near) dist[x, y] = it;
            }
        Func<IEnumerable<int>, int> dom = s => s.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key).DefaultIfEmpty(-1).First();
        var byD = new Dictionary<int, List<int>>();
        int top = rh, bot = -1;
        for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) {
            int d = dist[x, y]; if (d == 0) continue;
            if (d > 3) d = 3;
            List<int> l; if (!byD.TryGetValue(d, out l)) byD[d] = l = new List<int>();
            l.Add(t.Get(rx + x, ry + y));
            top = Math.Min(top, y); bot = Math.Max(bot, y);
        }
        if (bot < 0) { Build.Warn(op.File + " " + op.TimOff.ToString("X") + ": no original text found"); top = 0; bot = rh - 1; }
        int c1 = byD.ContainsKey(1) ? dom(byD[1]) : -1, c2 = byD.ContainsKey(2) ? dom(byD[2]) : -1, c3 = byD.ContainsKey(3) ? dom(byD[3]) : -1;
        int L = op.I("ol", -1);
        if (L < 0) {
            int interior = c3 >= 0 ? c3 : c2;
            if (c1 < 0 || interior < 0 || c1 == interior) L = 0;
            else if (c2 >= 0 && c3 >= 0 && c2 != c3 && c2 != c1) L = 2;
            else L = 1;
        }
        // per-row fill colour
        var rowFill = new int[rh];
        int fillAll = op.I("fill", -1);
        for (int y = 0; y < rh; y++) {
            var l = new List<int>();
            for (int x = 0; x < rw; x++) if (dist[x, y] > L) l.Add(t.Get(rx + x, ry + y));
            rowFill[y] = fillAll >= 0 ? fillAll : dom(l);
        }
        int lastF = -1; for (int y = 0; y < rh; y++) if (rowFill[y] >= 0) { lastF = rowFill[y]; break; }
        if (lastF < 0) lastF = fillAll >= 0 ? fillAll : (c3 >= 0 ? c3 : c2 >= 0 ? c2 : c1);
        for (int y = 0; y < rh; y++) { if (rowFill[y] < 0) rowFill[y] = lastF; else lastF = rowFill[y]; }
        int olOuter = op.I("olc", c1), olInner = op.I("olc2", L == 2 ? c2 : c1);
        // clear region
        if (op.S("clear", "1") == "1")
            for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) if (!keep.Contains(t.Get(rx + x, ry + y))) t.Set(rx + x, ry + y, bg(ry + y));
        // render lines
        if (op.P.ContainsKey("boxrgb")) { int bc = nearest(op.P["boxrgb"]); for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) t.Set(rx + x, ry + y, bc); }
        // render lines: Galmuri bitmap at native size only, single flat colour, no outline/shadow/gradient
        var lines = op.Text.Split('/');
        int n = lines.Length;
        int ttop = op.I("ttop", top), tbot = op.I("tbot", bot);
        float blockH = tbot - ttop + 1;
        float lineH = blockH / n;
        int margin = op.I("margin", 1);
        int availW = rw - 2 * margin;
        int fillC = op.I("fill", -1);
        if (fillC < 0) {
            var cnt = new Dictionary<int, int>();
            for (int y = Math.Max(0, top); y <= Math.Min(rh - 1, Math.Max(top, bot)); y++) { int v = rowFill[y]; int cc; cnt.TryGetValue(v, out cc); cnt[v] = cc + 1; }
            fillC = cnt.Count > 0 ? cnt.OrderByDescending(k => k.Value).First().Key : rowFill[0];
        }
        var sizes = new[] { new { K = "G14", H = 14 }, new { K = "G11", H = 11 }, new { K = "G9", H = 9 }, new { K = "G7", H = 7 } };
        string fk = null; int fh = 7;
        foreach (var sz in sizes) {
            if (sz.H > lineH + 0.01f && sz.K != "G7") continue;
            var b = GetBdf(sz.K); bool fits = true;
            foreach (var ln in lines) if (b.Line(ln, sz.H + 3, sz.H - 1).GetLength(0) - 1 > availW) { fits = false; break; }
            if (fits) { fk = sz.K; fh = sz.H; break; }
        }
        if (fk == null) { Build.Fail(op.File + " " + op.TimOff.ToString("X") + ": text too wide even at Galmuri7: " + op.Text); fk = "G7"; fh = 7; }
        var bdfL = GetBdf(fk);
        string align = op.S("align", "c");
        int dxo = op.I("dx", 0), dyo = op.I("dy", 0);
        for (int li = 0; li < n; li++) {
            var m = bdfL.Line(lines[li], fh + 3, fh - 1); int mw = m.GetLength(0) - 1, mh = m.GetLength(1);
            float cy = ttop + lineH * li + lineH / 2f;
            int oy = (int)Math.Round(cy - fh / 2f) + dyo;
            int ox = align == "l" ? margin + dxo : align == "r" ? rw - margin - mw + dxo : (int)Math.Round((rw - mw) / 2f) + dxo;
            if (op.I("outline", 0) == 1) { int oc = op.I("olc", c1);
                for (int y = -1; y <= mh; y++) for (int x = -1; x <= m.GetLength(0); x++) { bool hit = false; for (int dy = -1; dy <= 1 && !hit; dy++) for (int dx = -1; dx <= 1; dx++) { int xx = x + dx, yy = y + dy; if (xx >= 0 && yy >= 0 && xx < m.GetLength(0) && yy < mh && m[xx, yy]) { hit = true; break; } } if (hit) { int px = ox + x, py = oy + y; if (px >= 0 && py >= 0 && px < rw && py < rh) t.Set(rx + px, ry + py, oc); } } }
            for (int y = 0; y < mh; y++) for (int x = 0; x < m.GetLength(0); x++) if (m[x, y]) {
                int px = ox + x, py = oy + y; if (px < 0 || py < 0 || px >= rw || py >= rh) continue;
                t.Set(rx + px, ry + py, fillC);
            }
        }
        previews.Add(before); previews.Add(t.ToBitmap(2));
        previewNames.Add(op.File + " " + op.TimOff.ToString("X") + " L=" + L);
    }

    // ---------- NES opening ----------
    static int NesSlot(int c) { return c <= 0x56 ? c + 29 : c + 28; }
    public static void NesOpening() {
        string f = Build.Work + @"\tr\nes_ko.tsv"; if (!File.Exists(f)) return;
        var pages = File.ReadAllLines(f, Encoding.UTF8).Where(l => l.Trim() != "" && !l.StartsWith("#")).Select(l => l.Split('\t')[1]).ToList();
        // free codes
        var free = new List<int>();
        for (int c = 0x0B; c <= 0x55; c++) free.Add(c);
        for (int c = 0x59; c <= 0x79; c++) if (c != 0x5F && c != 0x62 && c != 0x63) free.Add(c);
        var map = new Dictionary<char, int>();
        map[' '] = 0x00; map['・'] = 0x63; map['8'] = 0x08; map['0'] = 0x05; map['2'] = 0x06;
        var outB = new List<byte>();
        foreach (var pg in pages) {
            var lines = pg.Split('/');
            for (int li = 0; li < lines.Length; li++) {
                string ln = lines[li].Replace("Dr.", "\u0001");
                int cells = 0;
                foreach (char ch in ln) {
                    if (ch == '\u0001') { outB.Add(0x02); outB.Add(0x04); cells += 2; continue; }
                    int code;
                    if (!map.TryGetValue(ch, out code)) {
                        if (free.Count == 0) { Build.Fail("NES: out of tiles at '" + ch + "'"); return; }
                        code = free[0]; free.RemoveAt(0); map[ch] = code;
                    }
                    outB.Add((byte)code); cells++;
                }
                if (cells > 24) Build.Fail("NES line too long (" + cells + "): " + lines[li]);
                if (li < lines.Length - 1) outB.Add(0xFD);
            }
            outB.Add(0xFF);
        }
        const int S = 0x378D5, E = 0x37A7D;
        if (outB.Count > E - S) { Build.Fail("NES text too long: " + outB.Count + " > " + (E - S)); return; }
        var rom = Build.F("22.bin");
        for (int i = S; i < E; i++) rom[i] = i - S < outB.Count ? outB[i - S] : (byte)0xFF;
        // tiles
        var t = new Tim(Build.F("02.bin"), 0x04022C);
        foreach (var kv in map) {
            if (kv.Value <= 0x0A || kv.Value == 0x63) continue;
            int n = NesSlot(kv.Value); int cx = 4 + 8 * (n % 31), cy = 36 + 8 * (n / 31);
            var gm = GetBdf("G7").Line(kv.Key.ToString(), 8, 7);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) t.Set(cx + x, cy + y, x < gm.GetLength(0) && gm[x, y] ? 1 : 0);
        }
        { int n = NesSlot(0x7A); int cx = 4 + 8 * (n % 31), cy = 36 + 8 * (n / 31); var gm = GetBdf("G7").Line("년", 8, 7); for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) t.Set(cx + x, cy + y, x < gm.GetLength(0) && gm[x, y] ? 1 : 0); }
        Build.Warn(string.Format("NES opening: {0} bytes / {1}, {2} glyph tiles, {3} free left", outB.Count, E - S, map.Count - 2, free.Count));
    }

    // NES title subtitle: screen tile (x,y) -> page4 (02.bin @2012C) cell
    public static void NesTitle() {
        var map = new Dictionary<int, int[]>();
        Action<int, int, int, int> M = (sx, sy, px, py) => map[sy * 1000 + sx] = new[] { px, py };
        int[] r72 = { 230, 238, 246 }; for (int i = 0; i < 3; i++) M(88 + 8 * i, 72, r72[i], 84);
        M(152, 72, 166, 100); M(160, 72, 174, 100); M(168, 72, 182, 100); M(184, 72, 198, 100); M(192, 72, 206, 100);
        for (int i = 0; i < 7; i++) M(88 + 8 * i, 80, 102 + 8 * i, 92);
        for (int i = 0; i < 6; i++) M(152 + 8 * i, 80, 38 + 8 * i, 108);
        M(88, 88, 230, 92); M(96, 88, 238, 92); M(104, 88, 246, 92);
        for (int i = 0; i < 4; i++) M(112 + 8 * i, 88, 6 + 8 * i, 100);
        for (int i = 0; i < 5; i++) M(152 + 8 * i, 88, 150 + 8 * i, 108);
        M(184, 88, 182, 108);
        var t = new Tim(Build.F("02.bin"), 0x2012C);
        foreach (var v in map.Values) for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) { int pv = t.Get(v[0] + x, v[1] + y); if (pv == 1 || pv == 2) t.Set(v[0] + x, v[1] + y, 0); }
        for (int y = 84; y < 116; y++) for (int x = 150; x < 172; x++) if (t.Get(x, y) == 1 || t.Get(x, y) == 2) t.Set(x, y, 0);
        foreach (var part in new[] { new { S = "새로운", R = 142, L = -1 }, new { S = "야망!!", R = -1, L = 152 } }) {
            var m = GetBdf("G11").Line(part.S, 14, 12);
            int w = m.GetLength(0) - 1, ox = part.L >= 0 ? part.L : part.R - w + 1, oy = 79;
            for (int y = 0; y < 14; y++) for (int x = 0; x < m.GetLength(0); x++) if (m[x, y]) {
                int sx = ox + x, sy = oy + y; int key = (sy / 8 * 8) * 1000 + (sx / 8 * 8); int[] v;
                if (!map.TryGetValue(key, out v)) { Build.Fail("NES title: no tile for " + sx + "," + sy); continue; }
                int tx = v[0] + sx % 8, ty = v[1] + sy % 8;
                if (t.Get(tx, ty) != 0) continue;
                t.Set(tx, ty, 1);
            }
        }
    }
    // erase text pixels (very light / very dark / listed colours) inside rect and fill from surrounding art
    public static void Erase(Op op) {
        var t = new Tim(Build.F(op.File), op.TimOff);
        var r = op.P["rect"].Split(',').Select(int.Parse).ToArray();
        int rx = r[0], ry = r[1], rw = r[2], rh = r[3];
        double hi = op.Fl("hi", 0.82f), lo = op.Fl("lo", 0.18f);
        var extra = new HashSet<int>();
        foreach (var s in op.S("colors", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) { var p = s.Split(',').Select(int.Parse).ToArray(); extra.Add((p[0] >> 3) | ((p[1] >> 3) << 5) | ((p[2] >> 3) << 10)); }
        var mark = new bool[rw, rh];
        for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) {
            var col = t.Col(t.Get(rx + x, ry + y)); double lum = (col.R * 0.3 + col.G * 0.59 + col.B * 0.11) / 255.0;
            int c15 = (col.R >> 3) | ((col.G >> 3) << 5) | ((col.B >> 3) << 10);
            int sat = Math.Max(col.R, Math.Max(col.G, col.B)) - Math.Min(col.R, Math.Min(col.G, col.B)); mark[x, y] = lum > hi || lum < lo || extra.Contains(c15) || sat < op.I("gray", 0) || (op.S("blue", "0") == "1" && col.B > col.R + 40) || (op.S("keepred", "0") == "1" && !(col.R > col.G + 50 && col.R > col.B + 50));
        }
        bool any = true;
        while (any) {
            any = false; var nm = (bool[,])mark.Clone();
            for (int y = 0; y < rh; y++) for (int x = 0; x < rw; x++) {
                if (!mark[x, y]) continue;
                var cnt = new Dictionary<int, int>();
                foreach (var d in new[] { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } }) {
                    int xx = x + d[0], yy = y + d[1];
                    int v;
                    if (xx < 0 || yy < 0 || xx >= rw || yy >= rh) { int ax = rx + xx, ay = ry + yy; if (ax < 0 || ay < 0 || ax >= t.W || ay >= t.H) continue; v = t.Get(ax, ay); }
                    else if (mark[xx, yy]) continue; else v = t.Get(rx + xx, ry + yy);
                    int n; cnt.TryGetValue(v, out n); cnt[v] = n + 1;
                }
                if (cnt.Count == 0) continue;
                t.Set(rx + x, ry + y, cnt.OrderByDescending(k => k.Value).First().Key); nm[x, y] = false; any = true;
            }
            mark = nm;
        }
    }
    static int NearestIdx(Tim t, Color want) {
        if (t.Bpp == 2) return (want.R >> 3) | ((want.G >> 3) << 5) | ((want.B >> 3) << 10) | 0x8000;
        int n = Math.Min(t.Bpp == 0 ? 16 : 256, t.ClutW); int best = 0; double bd = 1e18;
        for (int i = 0; i < n; i++) {
            int c = BitConverter.ToUInt16(t.D, t.ClutOff + i * 2); if (c == 0) continue;
            double dr = (c & 31) * 255 / 31 - want.R, dg = ((c >> 5) & 31) * 255 / 31 - want.G, db = ((c >> 10) & 31) * 255 / 31 - want.B;
            double dd = dr * dr * 0.3 + dg * dg * 0.59 + db * db * 0.11; if (dd < bd) { bd = dd; best = i; }
        }
        return best;
    }
    static Color Hex(string h) { return Color.FromArgb(Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
    // styled logo text: fill gradient + outward rings, skewed, from TTF
    public static void Logo(Op op) {
        var t = new Tim(Build.F(op.File), op.TimOff);
        var before = t.ToBitmap(2);
        var r = op.P["rect"].Split(',').Select(int.Parse).ToArray();
        int rx = r[0], ry = r[1], rw = r[2], rh = r[3];
        // 1. erase old letters (low saturation or blue) and inpaint
        if (op.S("erase", "1") == "1") {
            var e = new Op { File = op.File, TimOff = op.TimOff, Kind = "erase" };
            e.P["hi"] = "1.1"; e.P["lo"] = "-1"; e.P["gray"] = op.S("gray", "60");
            if (op.P.ContainsKey("blue")) e.P["blue"] = "1"; if (op.P.ContainsKey("keepred")) e.P["keepred"] = "1"; e.P["colors"] = op.S("colors", "");
            foreach (var er in op.S("erect", op.P["rect"]).Split(new[] { '/' })) { e.P["rect"] = er; Erase(e); }
            t = new Tim(Build.F(op.File), op.TimOff);
        }
        var fills = op.S("fill", "FFFFFF").Split(';').Select(Hex).ToArray();
        var rings = op.S("rings", "none") == "none" ? new Color[0] : op.S("rings", "").Split(';').Select(Hex).ToArray();
        int nr = rings.Length;
        float skew = op.Fl("skew", 0f);
        var fam = Fam("NSN-" + op.S("weight", "eHv"));
        int S = 4;
        // render text path
        using (var path = new GraphicsPath()) {
            path.AddString(op.Text, fam, 0, 100, new PointF(0, 0), StringFormat.GenericTypographic);
            using (var m = new Matrix(1, 0, -skew, 1, 0, 0)) path.Transform(m);
            var bb = path.GetBounds();
            float availW = rw - 4, availH = rh - 4;
            float k = Math.Min(availW / bb.Width, availH / bb.Height) * op.Fl("scale", 1f); float kx = op.S("stretch", "0") == "1" ? Math.Min(availW / bb.Width, k * op.Fl("maxstretch", 1.35f)) : k;
            float tw = bb.Width * kx, th = bb.Height * k;
            float ox = rx + (rw - tw) / 2f, oy = ry + (rh - th) / 2f;
            if (op.S("align", "c") == "l") ox = rx + nr + 1;
            using (var m = new Matrix()) { m.Translate(ox * S, oy * S); m.Scale(kx * S, k * S); m.Translate(-bb.X, -bb.Y); path.Transform(m); }
            int W = t.W, H = t.H;
            using (var bmp = new Bitmap(W * S, H * S, PixelFormat.Format32bppArgb)) using (var g = Graphics.FromImage(bmp)) {
                g.Clear(Color.Black); g.SmoothingMode = SmoothingMode.AntiAlias; g.FillPath(Brushes.White, path);
                var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var buf = new byte[data.Stride * bmp.Height]; System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buf, 0, buf.Length); bmp.UnlockBits(data);
                var core = new bool[W, H]; int top = H, bot = 0;
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
                    int s = 0; for (int yy = 0; yy < S; yy++) for (int xx = 0; xx < S; xx++) s += buf[(y * S + yy) * data.Stride + (x * S + xx) * 4 + 1];
                    core[x, y] = s >= S * S * 255 / 2; if (core[x, y]) { top = Math.Min(top, y); bot = Math.Max(bot, y); }
                }
                for (int b = 0; b < op.I("bold", 0); b++) { var nc = (bool[,])core.Clone(); for (int y = 0; y < H; y++) for (int x = 1; x < W; x++) if (core[x - 1, y]) nc[x, y] = true; for (int y = 1; y < H; y++) for (int x = 0; x < W; x++) if (core[x, y - 1]) nc[x, y] = true; core = nc; }
                // inner distance (from outside) for bevel rings drawn INSIDE the glyph; 1px outline outside
                var din = new int[W, H];
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) din[x, y] = core[x, y] ? 99 : 0;
                for (int it = 1; it <= nr; it++)
                    for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
                        if (din[x, y] != 99) continue;
                        bool near = false;
                        for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) { int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= W || yy >= H || din[xx, yy] == it - 1) { near = true; break; } }
                        if (near) din[x, y] = it;
                    }
                var outline = Hex(op.S("outline", "292929")); int olw = op.I("olw", 1);
                var dist = new int[W, H];
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
                    if (core[x, y]) { dist[x, y] = din[x, y] == 99 ? 0 : din[x, y]; continue; }
                    int dmin = 99;
                    for (int dy = -olw; dy <= olw; dy++) for (int dx = -olw; dx <= olw; dx++) { int xx = x + dx, yy = y + dy; if (xx >= 0 && yy >= 0 && xx < W && yy < H && core[xx, yy]) dmin = -1; }
                    dist[x, y] = dmin == -1 ? -1 : 99;
                }                var cache = new Dictionary<int, int>();
                Func<Color, int> idx = col => { int key = col.ToArgb(); int v; if (!cache.TryGetValue(key, out v)) { v = NearestIdx(t, col); cache[key] = v; } return v; };
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
                    int d = dist[x, y]; if (d == 99) continue;
                    Color col;
                    if (d == 0) {
                        double f = bot > top ? (double)(y - top) / (bot - top) : 0; double pos = f * (fills.Length - 1);
                        int i0 = (int)Math.Floor(pos); int i1 = Math.Min(fills.Length - 1, i0 + 1); double fr = pos - i0;
                        col = Color.FromArgb((int)(fills[i0].R + (fills[i1].R - fills[i0].R) * fr), (int)(fills[i0].G + (fills[i1].G - fills[i0].G) * fr), (int)(fills[i0].B + (fills[i1].B - fills[i0].B) * fr));
                    } else if (d < 0) col = outline; else col = rings[d - 1];
                    t.Set(x, y, idx(col));
                }
            }
        }
        previews.Add(before); previews.Add(t.ToBitmap(2)); previewNames.Add(op.File + " logo " + op.Text);
    }
    public static void Run() {
        NesOpening();
        string spec = Build.Work + @"\tr\gfx.tsv";
        if (!File.Exists(spec)) return;
        foreach (var raw in File.ReadAllLines(spec, Encoding.UTF8)) {
            var line = raw.Trim(); if (line == "" || line.StartsWith("#")) continue;
            var p = line.Split('\t');
            var op = new Op { File = p[0], TimOff = Convert.ToInt32(p[1], 16), Kind = p[2], Text = p[3] };
            for (int i = 4; i < p.Length; i++) { var kv = p[i].Split(new[] { '=' }, 2); if (kv.Length == 2) op.P[kv[0]] = kv[1]; }
            try { if (op.Kind == "label") Label(op); else if (op.Kind == "erase") Erase(op); else if (op.Kind == "logo") Logo(op); }
            catch (Exception e) { Build.Fail(op.File + " " + p[1] + ": " + e.Message); }
        }
        // preview sheet
        int W = 1400, x = 0, y = 0, rowH = 0;
        var pos = new List<Point>();
        for (int i = 0; i < previews.Count; i += 2) {
            int w = previews[i].Width * 2 + 12, h = previews[i].Height + 14;
            if (x + w > W) { x = 0; y += rowH + 4; rowH = 0; }
            pos.Add(new Point(x, y)); x += w + 8; rowH = Math.Max(rowH, h);
        }
        if (previews.Count == 0) return;
        using (var sheet = new Bitmap(W, y + rowH + 8)) using (var g = Graphics.FromImage(sheet)) {
            g.Clear(Color.FromArgb(30, 30, 30));
            var f = new Font("Arial", 7);
            for (int i = 0; i < previews.Count; i += 2) {
                var pt = pos[i / 2];
                g.DrawImage(previews[i], pt.X, pt.Y + 12); g.DrawImage(previews[i + 1], pt.X + previews[i].Width + 6, pt.Y + 12);
                g.DrawString(previewNames[i / 2], f, Brushes.Yellow, pt.X, pt.Y);
            }
            sheet.Save(Build.Work + @"\preview_gfx.png", ImageFormat.Png);
        }
    }
}
