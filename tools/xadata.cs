using System; using System.IO;
// xadata <bin> <lba0> <lba1> <out> <map> : concatenate form1 sectors' user data, write LBA map
class XaData { static void Main(string[] a) {
    using (var fs = File.OpenRead(a[0])) using (var o = File.Create(a[3])) using (var m = new StreamWriter(a[4])) {
        long l0 = long.Parse(a[1]), l1 = long.Parse(a[2]); var s = new byte[2352]; long outOff = 0; long runStart = -1, runOut = 0, prev = -2;
        for (long l = l0; l < l1; l++) {
            fs.Seek(l * 2352, SeekOrigin.Begin); fs.Read(s, 0, 2352);
            if ((s[18] & 0x20) != 0) continue;
            if (l != prev + 1) { if (runStart >= 0) m.WriteLine("{0} {1} {2:X}", runStart, prev, runOut); runStart = l; runOut = outOff; }
            o.Write(s, 24, 2048); outOff += 2048; prev = l;
        }
        if (runStart >= 0) m.WriteLine("{0} {1} {2:X}", runStart, prev, runOut);
    }
} }
