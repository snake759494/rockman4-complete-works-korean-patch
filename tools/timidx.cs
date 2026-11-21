using System; using System.IO;
// timidx <file> <timoff> : print palette index grid + clut colors
class TimIdx { static void Main(string[] a) {
    var d = File.ReadAllBytes(a[0]); var t = new Tim(d, Convert.ToInt32(a[1], 16));
    Console.WriteLine("{0}x{1} bpp{2} clut {3}x{4}", t.W, t.H, t.Bpp, t.ClutW, t.ClutH);
    if (t.ClutW > 0) { var s = ""; for (int i = 0; i < Math.Min(16, t.ClutW); i++) s += i.ToString("X") + ":" + BitConverter.ToUInt16(d, t.ClutOff + i * 2).ToString("X4") + " "; Console.WriteLine(s); }
    int maxW = a.Length > 2 ? int.Parse(a[2]) : 120;
    for (int y = 0; y < t.H; y++) { var s = ""; for (int x = 0; x < Math.Min(t.W, maxW); x++) { int v = t.Get(x, y); s += v == 0 ? "." : v.ToString("X1"); } Console.WriteLine(s); }
} }
