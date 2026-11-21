using System; using System.IO; using System.Collections.Generic;
class RleScan { static void Main(string[] a) {
    foreach (var f in a) {
        var d = File.ReadAllBytes(f);
        for (int o = 0; o + 16 < d.Length; o += 1) {
            if (d[o] != 2 || d[o + 1] != 0 || d[o + 3] != 0 || d[o + 2] < 1 || d[o + 2] > 32) continue;
            int size = BitConverter.ToInt32(d, o + 4); if (size < 1024 || size > 600000) continue;
            try {
                var s = new byte[Math.Min(d.Length - o, 700000)]; Array.Copy(d, o, s, 0, s.Length);
                List<byte[]> h; List<int> offs; var raw = Rle.Decode(s, out h, out offs);
                Console.WriteLine("{0} @{1:X}: type2 n={2} size={3:X}", Path.GetFileName(f), o, d[o + 2], size);
                File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(f), "..", "rle_" + Path.GetFileNameWithoutExtension(f) + "_" + o.ToString("X") + ".bin"), raw);
            } catch { }
        }
    }
} }

