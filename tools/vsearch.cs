using System; using System.IO; using System.Drawing;
// vsearch <vram.png> <x> <y> <nwords> <files...> : search files for 15-bit masked halfword run from VRAM screenshot
class VSearch { static void Main(string[] a) {
    var im = new Bitmap(a[0]); int x0 = int.Parse(a[1]), y0 = int.Parse(a[2]), n = int.Parse(a[3]);
    var w = new int[n];
    for (int i = 0; i < n; i++) { var c = im.GetPixel(x0 + i, y0); int r = (int)Math.Round(c.R * 31 / 255.0), g = (int)Math.Round(c.G * 31 / 255.0), b = (int)Math.Round(c.B * 31 / 255.0); w[i] = r | g << 5 | b << 10; }
    Console.WriteLine("words: " + string.Join(" ", Array.ConvertAll(w, v => v.ToString("X4"))));
    for (int f = 4; f < a.Length; f++) {
        var d = File.ReadAllBytes(a[f]);
        for (int o = 0; o + n * 2 <= d.Length; o++) {
            bool ok = true;
            for (int i = 0; i < n && ok; i++) if ((BitConverter.ToUInt16(d, o + i * 2) & 0x7FFF) != w[i]) ok = false;
            if (ok) Console.WriteLine("{0} @{1:X}", Path.GetFileName(a[f]), o);
        }
    }
} }
