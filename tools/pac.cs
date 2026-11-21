using System;
using System.IO;

// pac list <pac> | pac split <pac> <outdir>
class Pac {
    static void Main(string[] a) {
        byte[] d = File.ReadAllBytes(a[1]);
        int n = BitConverter.ToInt32(d, 0);
        for (int i = 0; i < n; i++) {
            int sec = BitConverter.ToInt32(d, 8 + i * 8), sz = BitConverter.ToInt32(d, 12 + i * 8);
            int off = sec * 2048;
            string sig = "";
            for (int k = 0; k < 16 && off + k < d.Length; k++) sig += d[off + k].ToString("X2") + " ";
            Console.WriteLine("{0,2} sec={1,5} off={2,8:X} size={3,8:X}  {4}", i, sec, off, sz, sig);
            if (a[0] == "split") {
                Directory.CreateDirectory(a[2]);
                byte[] o = new byte[sz];
                Array.Copy(d, off, o, 0, Math.Min(sz, d.Length - off));
                File.WriteAllBytes(Path.Combine(a[2], i.ToString("D2") + ".bin"), o);
            }
        }
    }
}
