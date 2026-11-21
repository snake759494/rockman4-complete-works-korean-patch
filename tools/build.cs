using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Collections.Generic;

public class Build {
    public static string Root = @"D:\ps1\록맨4_컴플리트웍스";
    public static string Work = Root + @"\work";
    static List<string> Errors = new List<string>();
    static StreamWriter Log;
    public static void Warn(string s) { Log.WriteLine("WARN " + s); }
    public static void Fail(string s) { Errors.Add(s); }

    // ---------- glyph model ----------
    public const int BLANK = 0xFD;
    static Dictionary<char, int> Fixed = new Dictionary<char, int>();
    public static Dictionary<char, int> Map = new Dictionary<char, int>(); // char -> plane*256+glyph
    public static int Bpp; // bits per plane
    public static int Planes;

    static void InitFixed() {
        for (int i = 0; i < 10; i++) Fixed[(char)('0' + i)] = i;
        Fixed['('] = 0x0A; Fixed[')'] = 0x0B; Fixed['?'] = 0x0C; Fixed['!'] = 0x0D;
        Fixed['「'] = 0x0E; Fixed['」'] = 0x0F; Fixed['、'] = 0x10; Fixed['。'] = 0x11;
        Fixed['□'] = 0xB6; Fixed['△'] = 0xB7; Fixed['×'] = 0xB8; Fixed['○'] = 0xB9;
        Fixed['…'] = 0xBE; Fixed['▼'] = 0xBF; Fixed['ー'] = 0xFC; Fixed[' '] = BLANK; Fixed['_'] = BLANK;
    }
    static readonly int[] KeepOriginal = { 0,1,2,3,4,5,6,7,8,9,0x0A,0x0B,0x0C,0x0D,0x0E,0x0F,0x10,0x11,
        0xB6,0xB7,0xB8,0xB9,0xBA,0xBB,0xBC,0xBD,0xBE,0xBF,0xFC };

    static List<int> Plane0Slots() {
        var l = new List<int>();
        for (int g = 0x12; g <= 0xB5; g++) l.Add(g);
        for (int g = 0xC0; g <= 0xEF; g++) l.Add(g);
        for (int g = 0xF4; g <= 0xFB; g++) l.Add(g);
        return l;
    }
    static List<int> PlaneNSlots() {
        var l = new List<int>();
        for (int g = 0x00; g <= 0xEF; g++) l.Add(g);
        for (int g = 0xF4; g <= 0xFE; g++) if (g != BLANK) l.Add(g);
        return l;
    }

    // ---------- translation records ----------
    public class Rec { public string Id, File, Ja, Ko; public int Off, Max, Bias = -1, LineW = 8, MaxLines = 3; public bool Bytes; }
    static List<Rec> Recs = new List<Rec>();

    static Dictionary<string, string> LoadKo(string f) {
        var d = new Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(f, Encoding.UTF8)) {
            if (line.Trim() == "" || line.StartsWith("#")) continue;
            var p = line.Split('\t');
            d[p[0]] = p.Length > 1 ? p[1] : "";
        }
        return d;
    }
    static void LoadByteRecs(string ja, string ko) {
        var k = LoadKo(ko);
        foreach (var line in File.ReadAllLines(ja, Encoding.UTF8)) {
            var p = line.Split('\t'); if (p.Length < 5) continue;
            var r = new Rec { Id = p[0], File = p[1], Off = Convert.ToInt32(p[2], 16), Max = int.Parse(p[3]), Ja = p[4], Bytes = true };
            if (!k.TryGetValue(r.Id, out r.Ko)) { Errors.Add("missing ko " + r.Id); continue; }
            Recs.Add(r);
        }
    }
    static void LoadHwRecs(string ja, string ko) {
        var k = LoadKo(ko);
        foreach (var line in File.ReadAllLines(ja, Encoding.UTF8)) {
            var p = line.Split('\t'); if (p.Length < 6) continue;
            var r = new Rec { Id = p[0], File = p[1], Off = Convert.ToInt32(p[2], 16), Max = int.Parse(p[3]), Bias = int.Parse(p[4]), Ja = p[5] };
            if (!k.TryGetValue(r.Id, out r.Ko)) { continue; } // untranslated garbage entries are skipped
            if (r.Id.StartsWith("D") && r.Id.EndsWith("T")) { r.LineW = 16; r.MaxLines = 4; }
            Recs.Add(r);
        }
    }

    // layout -> list of cells (char) with '\uFFFF' = page break
    const char PAGE = '\uFFFF';
    public static List<char> Layout(Rec r) {
        var cells = new List<char>();
        string t = r.Ko;
        if (!t.Contains("/") && !t.Contains("||")) { foreach (char c in t) cells.Add(c == '^' ? '\uFFFE' : c); return cells; }
        var pages = t.Split(new[] { "||" }, StringSplitOptions.None).Select(s => s.Trim()).ToList();
        for (int pi = 0; pi < pages.Count; pi++) {
            var lines = pages[pi].Length == 0 ? new string[0] : pages[pi].Split('/');
            if ((r.Bytes || r.LineW == 16) && lines.Length > r.MaxLines) Errors.Add(r.Id + ": page " + pi + " has " + lines.Length + " lines");
            for (int li = 0; li < lines.Length; li++) {
                string ln = lines[li];
                if (ln.Length > r.LineW) Errors.Add(r.Id + ": line too long (" + ln.Length + ") '" + ln + "'");
                foreach (char c in ln) cells.Add(c);
                bool last = (pi == pages.Count - 1) && (li == lines.Length - 1);
                if (!last) for (int k = ln.Length; k < r.LineW; k++) cells.Add('_');
            }
            if (pi < pages.Count - 1) cells.Add(PAGE);
        }
        return cells;
    }

    // ---------- allocation ----------
    static void Allocate() {
        InitFixed();
        var freq = new Dictionary<char, double>();
        foreach (var r in Recs) {
            double w = r.Id.StartsWith("W") ? 100000 : r.Bytes ? 10 : 1;
            foreach (char c in Layout(r)) {
                if (c == PAGE || c == '\uFFFE' || Fixed.ContainsKey(c)) continue;
                double f; freq.TryGetValue(c, out f); freq[c] = f + w;
            }
        }
        var order = freq.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => kv.Key).ToList();
        var p0 = Plane0Slots(); var pn = PlaneNSlots();
        int n = order.Count;
        if (n <= p0.Count + pn.Count) { Bpp = 2; Planes = 2; }
        else if (n <= p0.Count + 3 * pn.Count) { Bpp = 1; Planes = 4; }
        else { Errors.Add("too many glyphs: " + n); Bpp = 1; Planes = 4; }
        foreach (var kv in Fixed) Map[kv.Key] = kv.Value;
        int idx = 0;
        foreach (char c in order) {
            if (idx < p0.Count) Map[c] = p0[idx];
            else {
                int k = idx - p0.Count; int plane = 1 + k / pn.Count;
                if (plane >= Planes) { Errors.Add("no slot for " + c); continue; }
                Map[c] = plane * 256 + pn[k % pn.Count];
            }
            idx++;
        }
        Log.WriteLine("unique chars {0}, planes {1} x {2}bpp", n, Planes, Bpp);
    }

    // ---------- font ----------
    static byte[] Lv = new byte[256 * 256 * 4]; // level per plane: [plane][y][x]
    static int GetLv(int p, int x, int y) { return Lv[(p * 256 + y) * 256 + x]; }
    static void SetLv(int p, int x, int y, int v) { Lv[(p * 256 + y) * 256 + x] = (byte)v; }

    static void BuildFont(byte[] tim) {
        // original clut & pixels
        var clut = new int[16];
        for (int i = 0; i < 16; i++) clut[i] = BitConverter.ToUInt16(tim, 20 + i * 2);
        int img = 0x40;
        int maxLv = (1 << Bpp) - 1;
        Func<int, int, int> origIdx = (x, y) => { int b = tim[img + y * 128 + x / 2]; return (x & 1) == 0 ? b & 15 : b >> 4; };
        Func<int, double> lumOf = c => ((c & 31) * 0.3 + ((c >> 5) & 31) * 0.59 + ((c >> 10) & 31) * 0.11) / 31.0;
        foreach (int g in KeepOriginal) {
            int gx = (g % 16) * 16, gy = (g / 16) * 16;
            // rank distinct luminances in this glyph -> levels 1..maxLv
            var lums = new SortedSet<double>();
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { int ci = origIdx(gx + x, gy + y); if (ci != 0) lums.Add(lumOf(clut[ci])); }
            var ls = lums.ToList();
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) {
                int ci = origIdx(gx + x, gy + y);
                int lv = 0;
                if (ci != 0) {
                    double lum = lumOf(clut[ci]);
                    if (ls.Count <= maxLv) lv = maxLv - (ls.Count - 1 - ls.IndexOf(lum));
                    else { int rank = ls.IndexOf(lum); lv = 1 + rank * maxLv / ls.Count; if (lv > maxLv) lv = maxLv; }
                }
                SetLv(0, gx + x, gy + y, lv);
            }
        }
        // render korean / others with Galmuri14 bitmap (native size, no scaling, single level)
        var bdf = Extra.GetBdf("G14");
        int maxLvK = (1 << Bpp) - 1;
        foreach (var kv in Map) {
            if (Fixed.ContainsKey(kv.Key)) continue;
            int plane = kv.Value >> 8, gl = kv.Value & 0xFF;
            Bdf.G gg; if (!bdf.Glyphs.TryGetValue(kv.Key, out gg)) { Errors.Add("Galmuri14 lacks U+" + ((int)kv.Key).ToString("X4")); continue; }
            var m = bdf.Line(kv.Key.ToString(), 16, 14);
            int w = m.GetLength(0), ox = (16 - gg.Dw) / 2; if (ox < 0) ox = 0;
            int gx = (gl % 16) * 16, gy = (gl / 16) * 16;
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) {
                int sx2 = x - ox; bool on = sx2 >= 0 && sx2 < w && m[sx2, y];
                SetLv(plane, gx + x, gy + y, on ? maxLvK : 0);
            }
        }
        // compose 4bpp image
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) {
            int v = 0;
            for (int p = 0; p < Planes; p++) v |= GetLv(p, x, y) << (p * Bpp);
            int o = img + y * 128 + x / 2;
            if ((x & 1) == 0) tim[o] = (byte)((tim[o] & 0xF0) | v); else tim[o] = (byte)((tim[o] & 0x0F) | (v << 4));
        }
        // CLUT rows for each plane at image row 240+p, halfwords 0..15
        int[] col2 = { 0x0000, 0x2529, 0x56B5, 0x7FFF };
        int[] col1 = { 0x0000, 0x7FFF };
        for (int p = 0; p < Planes; p++) {
            for (int i = 0; i < 16; i++) {
                int lv = (i >> (p * Bpp)) & ((1 << Bpp) - 1);
                int c = Bpp == 2 ? col2[lv] : col1[lv];
                int o = img + (240 + p) * 128 + i * 2;
                tim[o] = (byte)c; tim[o + 1] = (byte)(c >> 8);
            }
        }
        // preview
        using (var pv = new Bitmap(256 * 2 * Planes, 256 * 2)) {
            for (int p = 0; p < Planes; p++) for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) {
                int lv = GetLv(p, x, y); int c = lv * 255 / ((1 << Bpp) - 1);
                var col = Color.FromArgb(c, c, c / 2 + (lv == 0 ? 60 : 0));
                for (int k = 0; k < 4; k++) pv.SetPixel(p * 512 + x * 2 + (k & 1), y * 2 + (k >> 1), col);
            }
            pv.Save(Work + @"\preview_font.png", ImageFormat.Png);
        }
    }

    // ---------- encoding ----------
    static List<byte> EncodeBytes(Rec r) {
        var o = new List<byte>();
        foreach (char c in Layout(r)) {
            if (c == PAGE) { o.Add(0xFF); continue; }
            int v; if (!Map.TryGetValue(c, out v)) { Errors.Add(r.Id + ": unmapped '" + c + "'"); continue; }
            int p = v >> 8, g = v & 0xFF;
            if (p == 0) o.Add((byte)(g + 1)); else { o.Add((byte)(0xF0 + p)); o.Add((byte)(g + 1)); }
        }
        return o;
    }
    static List<int> EncodeHw(Rec r) {
        var o = new List<int>();
        foreach (char c in Layout(r)) {
            if (c == '\uFFFE') { o.Add(0xFFFE); continue; }
            if (c == PAGE) { Errors.Add(r.Id + ": page break in hw"); continue; }
            int v; if (!Map.TryGetValue(c, out v)) { Errors.Add(r.Id + ": unmapped '" + c + "'"); continue; }
            o.Add(v + r.Bias);
        }
        return o;
    }

    static Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
    public static byte[] F(string name) {
        byte[] b;
        if (!Files.TryGetValue(name, out b)) {
            string src = name.StartsWith("SLPS") ? Work + @"\ext\" + name : Work + @"\pac\" + name;
            b = File.ReadAllBytes(src); Files[name] = b;
        }
        return b;
    }

    static void WriteNavi() {
        // repack 17.bin navi block and relocate pointers in 18.bin
        var d = F("17.bin"); var p18 = F("18.bin");
        const int BLK = 0x95F8, END = 0xACEE; // END = last allowed terminator position
        const uint BASE = 0x8006E5A0;
        var navi = Recs.Where(r => r.Bytes && r.File == "17.bin").OrderBy(r => r.Off).ToList();
        var remap = new Dictionary<uint, uint>();
        int pos = BLK;
        var blob = new List<byte>();
        foreach (var r in navi) {
            var e = EncodeBytes(r);
            while ((BLK + blob.Count) % 4 != 0) blob.Add(0);
            remap[BASE + (uint)r.Off] = BASE + (uint)(BLK + blob.Count);
            blob.AddRange(e); blob.Add(0);
        }
        if (BLK + blob.Count - 1 > END) { Errors.Add(string.Format("navi block overflow: {0} bytes over", BLK + blob.Count - 1 - END)); return; }
        Log.WriteLine("navi block used {0} / {1} bytes", blob.Count, END - BLK + 1);
        for (int i = BLK; i <= END; i++) d[i] = i - BLK < blob.Count ? blob[i - BLK] : (byte)0;
        int n = 0;
        for (int i = 0; i + 4 <= p18.Length; i += 4) {
            uint w = BitConverter.ToUInt32(p18, i);
            if (w >= BASE + BLK && w <= BASE + END) {
                uint nw; if (!remap.TryGetValue(w, out nw)) { Errors.Add(string.Format("18.bin ptr {0:X8} at {1:X} not a message start", w, i)); continue; }
                BitConverter.GetBytes(nw).CopyTo(p18, i); n++;
            }
        }
        Log.WriteLine("navi pointers relocated: {0}", n);
        if (n != 125) Errors.Add("unexpected navi pointer count " + n);
    }

    static void WriteStrings() {
        WriteNavi();
        foreach (var r in Recs) {
            if (r.Bytes && r.File == "17.bin") continue;
            var d = F(r.File);
            if (r.Bytes) {
                var e = EncodeBytes(r);
                if (e.Count > r.Max) { Errors.Add(string.Format("{0}: {1} bytes > max {2}", r.Id, e.Count, r.Max)); continue; }
                for (int i = 0; i <= r.Max; i++) d[r.Off + i] = i < e.Count ? e[i] : (byte)0;
            } else {
                var e = EncodeHw(r);
                if (e.Count > r.Max) { Errors.Add(string.Format("{0}: {1} hw > max {2}", r.Id, e.Count, r.Max)); continue; }
                int term = r.Bias == 0 ? 0xFFFF : 0;
                for (int i = 0; i < e.Count; i++) { d[r.Off + i * 2] = (byte)e[i]; d[r.Off + i * 2 + 1] = (byte)(e[i] >> 8); }
                // terminator right after; pad remaining with terminator value (b1) / keep original zeros area
                for (int i = e.Count; i <= r.Max; i++) {
                    int v = (i == e.Count) ? term : (r.Bias == 0 ? 0xFFFF : 0);
                    d[r.Off + i * 2] = (byte)v; d[r.Off + i * 2 + 1] = (byte)(v >> 8);
                }
            }
        }
    }

    // ---------- code patches ----------
    static void Put(byte[] d, int off, params uint[] w) { for (int i = 0; i < w.Length; i++) { BitConverter.GetBytes(w[i]).CopyTo(d, off + i * 4); } }
    static void Expect(byte[] d, int off, uint w, string what) { if (BitConverter.ToUInt32(d, off) != w) Errors.Add("patch precheck failed: " + what); }

    static void PatchCode() {
        // EXE glyph cell function 0x8004209C
        var exe = F("SLPS_023.24");
        Func<uint, int> eo = a => (int)(a - 0x80010000 + 0x800);
        Expect(exe, eo(0x80042148), 0x00072400, "exe sll a0,a3,16");
        Expect(exe, eo(0x8004222C), 0x8D2B0010, "exe lw t3,16(t1)");
        uint b = 0x80042148;
        Put(exe, eo(b),
            M.sll("a0", "a3", 16),
            M.sra("a0", "a0", 16),
            M.lui("at", 0x8006),
            M.addiu("at", "at", unchecked((short)0x8B28)),
            M.bne("t1", "at", b + 16, 0x8004217C),
            M.lw("t8", 16, "t1"),
            M.andi("at", "a0", 0xFF),
            M.srl("t8", "a0", 8),
            M.sll("t8", "t8", 16),
            M.addu("a0", "at", "zero"),
            M.lui("at", 0x01F0),
            M.addu("t8", "t8", "at"),
            M.ori("t8", "t8", 0x0180),
            M.div("a0", "t2"),
            M.mflo("a0"),
            M.mfhi("v0"));
        Put(exe, eo(0x8004222C), M.addu("t3", "t8", "zero"));

        // lab converter in 12.bin (base 0x8006E5A0)
        var o12 = F("12.bin");
        Func<uint, int> lo = a => (int)(a - 0x8006E5A0);
        Expect(o12, lo(0x800769A4), 0x144B0003, "12 loop head");
        Expect(o12, lo(0x80076A18), 0xA4440000, "12 post");
        uint L = 0x800769A4;
        var w12 = new uint[] {
            M.addiu("v0","v0",-1),                 // A4
            M.addiu("v1","v0",-0xF0),              // A8
            M.sltiu("v1","v1",3),                  // AC
            M.beq("v1","zero",L+0x0C,L+0x2C),      // B0 -> D0
            M.addiu("a1","a1",1),                  // B4
            M.lbu("v1",0,"a1"),                    // B8
            M.andi("v0","v0",3),                   // BC
            M.sll("v0","v0",8),                    // C0
            M.addu("v0","v0","v1"),                // C4
            M.addiu("v0","v0",255),                // C8
            M.addiu("a1","a1",1),                  // CC
            M.lw("v1",3348,"t0"),                  // D0 STORE
            M.sh("v0",0,"a0"),                     // D4
            M.addiu("v1","v1",1),                  // D8
            M.slti("v0","v1",33),                  // DC
            M.beq("v0","zero",L+0x3C,L+0x48),      // E0 -> EC
            M.addiu("a0","a0",2),                  // E4
            M.sw("v1",0,"t1"),                     // E8
            M.lbu("v0",0,"a1"),                    // EC
            M.addiu("a3","a3",1),                  // F0
            M.bne("v0","zero",L+0x50,L),           // F4
            M.nop(),                               // F8
            M.addiu("v1","zero",-1),               // FC
            M.sh("v1",0,"a0"),                     // A00
            M.sh("v1",2,"a0"),                     // A04
            M.addiu("s0","sp",24),                 // A08
            M.nop(), M.nop(), M.nop(), M.nop()     // A0C..A18
        };
        // wait: original B4 consumed a1++ for normal path; normal path: a1 advanced at B4 only -> correct.
        Put(o12, lo(L), w12);

        // navi converter in 18.bin (listing base 0x80100000)
        var o18 = F("18.bin");
        Func<uint, int> no = a => (int)(a - 0x80100000);
        Expect(o18, no(0x8018C140), 0x14670003, "18 loop head");
        Expect(o18, no(0x8018C1C8), 0xA4480000, "18 post sh");
        Expect(o18, no(0x8018C1CC), 0x3C028007, "18 after");
        uint N = 0x8018C140;
        uint STORE = N + 0x38, FFP = 0x8018C1C4;
        var w18 = new uint[] {
            M.beq("v1","a3",N,FFP),               // 140
            M.addiu("t1","t1",1),                 // 144
            M.addiu("v0","v1",-1),                // 148
            M.addiu("t2","v1",-0xF1),             // 14C
            M.sltiu("t2","t2",3),                 // 150
            M.beq("t2","zero",N+0x14,STORE),      // 154
            M.lw("t2",4,"a1"),                    // 158
            M.andi("v0","v0",3),                  // 15C
            M.addu("t2","t2","t1"),               // 160
            M.lbu("t2",0,"t2"),                   // 164
            M.addiu("t1","t1",1),                 // 168
            M.sll("v0","v0",8),                   // 16C
            M.addu("v0","v0","t2"),               // 170
            M.addiu("v0","v0",255),               // 174
            M.sh("v0",0,"a0"),                    // 178 STORE
            M.lw("v0",4,"a1"),                    // 17C
            M.addiu("a0","a0",2),                 // 180
            M.addu("v0","v0","t1"),               // 184
            M.lbu("v1",0,"v0"),                   // 188
            M.addiu("t0","zero",-1),              // 18C
            M.bne("v1","zero",N+0x50,N),          // 190
            M.sh("t0",0,"a0"),                    // 194
            M.sh("t0",2,"a0"),                    // 198
            M.lui("a0",0x8011),                   // 19C
            M.addiu("a0","a0",21152),             // 1A0
            M.lui("a1",0x8006),                   // 1A4
            M.addiu("a1","a1",unchecked((short)0x8B28)), // 1A8
            M.addiu("a2","zero",16),              // 1AC
            M.addu("a3","a2","zero"),             // 1B0
            M.lui("v1",0x8011),                   // 1B4
            M.addiu("v1","v1",21192),             // 1B8
            M.beq("zero","zero",N+0x7C,0x8018C1CC),// 1BC -> 1CC
            M.lui("s0",0x8011),                   // 1C0
            M.beq("zero","zero",FFP,STORE),       // 1C4 FFP
            M.addiu("v0","zero",-1)               // 1C8
        };
        Put(o18, no(N), w18);
    }

    // ---------- main ----------
    public static void Main(string[] a) { try { Main2(a); } catch (Exception ex) { File.WriteAllText(Work + @"\build_err.txt", ex.ToString()); Environment.Exit(3); } }
    static void Main2(string[] a) {
        Directory.CreateDirectory(Work + @"\out");
        Log = new StreamWriter(Work + @"\build_log.txt", false, new UTF8Encoding(false));
        LoadByteRecs(Work + @"\tr\navi_ja.tsv", Work + @"\tr\navi_ko.tsv");
        LoadByteRecs(Work + @"\tr\lab_ja.tsv", Work + @"\tr\lab_ko.tsv");
        LoadByteRecs(Work + @"\tr\weapon_ja.tsv", Work + @"\tr\weapon_ko.tsv");
        LoadHwRecs(Work + @"\tr\hw_ja.tsv", Work + @"\tr\hw_ko.tsv");
        LoadHwRecs(Work + @"\tr\db_ja.tsv", Work + @"\tr\db_ko.tsv");
        LoadHwRecs(Work + @"\tr\pocket_ja.tsv", Work + @"\tr\pocket_ko.tsv");
        // boot overlay (00.bin, RLE) with nested warning TIM
        List<byte[]> bootH, timH; List<int> bootO, timO;
        var boot = Rle.Decode(F("00.bin"), out bootH, out bootO);
        var innerSrc = new byte[boot.Length - 0x1B74]; Array.Copy(boot, 0x1B74, innerSrc, 0, innerSrc.Length);
        var warnTim = Rle.Decode(innerSrc, out timH, out timO);
        int innerLen = Rle.Encode(warnTim, innerSrc, timH, timO).Length;
        Files["boot.bin"] = boot; Files["warn.tim"] = warnTim;
        LoadHwRecs(Work + @"\tr\boot_ja.tsv", Work + @"\tr\boot_ko.tsv");
        foreach (var r in Recs) if (r.Id.StartsWith("B0_")) { r.LineW = 14; r.MaxLines = 4; }
        Allocate();
        var tim = F("03.bin");
        BuildFont(tim);
        WriteStrings();
        PatchCode();
        Extra.Run(); // graphics / NES etc.
        {
            var innerNew = Rle.Encode(Files["warn.tim"], innerSrc, timH, timO);
            if (innerNew.Length > innerLen) Errors.Add("warning TIM recompressed too big: " + innerNew.Length + " > " + innerLen);
            else {
                for (int i = 0; i < innerLen; i++) boot[0x1B74 + i] = i < innerNew.Length ? innerNew[i] : (byte)0;
                var origPac = File.ReadAllBytes(Work + @"\pac\00.bin");
                var outer = Rle.Encode(boot, origPac, bootH, bootO);
                Log.WriteLine("boot: inner {0}/{1}, outer {2}/{3}", innerNew.Length, innerLen, outer.Length, origPac.Length);
                if (outer.Length > origPac.Length) Errors.Add("boot overlay recompressed too big: " + outer.Length + " > " + origPac.Length);
                else { var o = new byte[origPac.Length]; Array.Copy(outer, o, outer.Length); Files["00.bin"] = o; }
            }
            Files.Remove("boot.bin"); Files.Remove("warn.tim");
        }
        // report
        foreach (var e in Errors) { Log.WriteLine("ERROR " + e); }
        var mapOut = new StringBuilder();
        foreach (var kv in Map.OrderBy(k => k.Value)) mapOut.AppendFormat("{0:X3}\t{1}\n", kv.Value, kv.Key);
        File.WriteAllText(Work + @"\charmap.txt", mapOut.ToString(), new UTF8Encoding(false));
        Log.Close();
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(File.ReadAllText(Work + @"\build_log.txt"));
        if (Errors.Count > 0) { Console.WriteLine("BUILD FAILED: " + Errors.Count + " errors"); Environment.Exit(1); }
        foreach (var kv in Files) File.WriteAllBytes(Work + @"\out\" + kv.Key, kv.Value);
        Console.WriteLine("files written: " + string.Join(", ", Files.Keys));
    }
}


