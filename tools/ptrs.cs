using System; using System.IO;
// ptrs <lohex> <hihex> <files...> : list word-aligned values in [lo,hi)
class Ptrs { static void Main(string[] a) {
    uint lo = Convert.ToUInt32(a[0], 16), hi = Convert.ToUInt32(a[1], 16);
    for (int fi = 2; fi < a.Length; fi++) { var d = File.ReadAllBytes(a[fi]); int c = 0;
        for (int i = 0; i + 4 <= d.Length; i += 4) { uint w = BitConverter.ToUInt32(d, i); if (w >= lo && w < hi) { if (c < 6 || a.Length < 4) Console.WriteLine("{0} @{1:X}: {2:X8}", Path.GetFileName(a[fi]), i, w); c++; } }
        if (c > 0) Console.WriteLine("{0}: total {1}", Path.GetFileName(a[fi]), c); } } }
