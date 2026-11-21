using System;
using System.IO;
using System.Collections.Generic;

// disc <srcbin> <dstbin> : copy original bin, then write work\out files into their LBAs (same size)
// DATA.PAC entries in work\out\NN.bin are packed back into DATA.PAC first.
class Disc {
    static uint[] EDC = new uint[256];
    static byte[] EF = new byte[256], EB = new byte[256];
    static void Init() {
        for (int i = 0; i < 256; i++) {
            int j = ((i << 1) ^ ((i & 0x80) != 0 ? 0x11D : 0)) & 0xFF;
            EF[i] = (byte)j; EB[i ^ j] = (byte)i;
            uint e = (uint)i; for (int k = 0; k < 8; k++) e = (e >> 1) ^ ((e & 1) != 0 ? 0xD8018001u : 0);
            EDC[i] = e;
        }
    }
    static void EccBlock(byte[] s, int src, int maj, int min, int mm, int mi, int dst) {
        int size = maj * min;
        for (int major = 0; major < maj; major++) {
            int index = (major >> 1) * mm + (major & 1);
            byte a = 0, b = 0;
            for (int m = 0; m < min; m++) {
                byte t = s[src + index]; index += mi; if (index >= size) index -= size;
                a ^= t; b ^= t; a = EF[a];
            }
            a = EB[EF[a] ^ b];
            s[dst + major] = a; s[dst + major + maj] = (byte)(a ^ b);
        }
    }
    static void Fix(byte[] s) {
        uint e = 0; for (int i = 16; i < 2072; i++) e = (e >> 8) ^ EDC[(e ^ s[i]) & 0xFF];
        BitConverter.GetBytes(e).CopyTo(s, 2072);
        byte[] h = new byte[4]; Array.Copy(s, 12, h, 0, 4);
        for (int i = 12; i < 16; i++) s[i] = 0;
        EccBlock(s, 12, 86, 24, 2, 86, 0x81C);
        EccBlock(s, 12, 52, 43, 86, 88, 0x8C8);
        Array.Copy(h, 0, s, 12, 4);
    }
    static int changed = 0;
    static void WriteFile(FileStream fs, long lba, byte[] data) {
        int nsec = (data.Length + 2047) / 2048;
        var sec = new byte[2352];
        for (int i = 0; i < nsec; i++) {
            fs.Seek((lba + i) * 2352, SeekOrigin.Begin); fs.Read(sec, 0, 2352);
            bool diff = false;
            for (int k = 0; k < 2048; k++) {
                int o = i * 2048 + k; byte v = o < data.Length ? data[o] : sec[24 + k];
                if (sec[24 + k] != v) { sec[24 + k] = v; diff = true; }
            }
            if (!diff) continue;
            Fix(sec); changed++;
            fs.Seek((lba + i) * 2352, SeekOrigin.Begin); fs.Write(sec, 0, 2352);
        }
    }
    static void Main(string[] a) {
        Init();
        string work = @"D:\ps1\록맨4_컴플리트웍스\work";
        // pack DATA.PAC
        byte[] pac = File.ReadAllBytes(work + @"\ext\DATA.PAC");
        int n = BitConverter.ToInt32(pac, 0);
        for (int i = 0; i < n; i++) {
            string f = work + @"\out\" + i.ToString("D2") + ".bin";
            if (!File.Exists(f)) continue;
            int sec = BitConverter.ToInt32(pac, 8 + i * 8), sz = BitConverter.ToInt32(pac, 12 + i * 8);
            byte[] d = File.ReadAllBytes(f);
            if (d.Length != sz) throw new Exception("size mismatch " + f);
            Array.Copy(d, 0, pac, sec * 2048, sz);
            Console.WriteLine("packed " + i);
        }
        if (!File.Exists(a[1]) || new FileInfo(a[1]).Length != new FileInfo(a[0]).Length) File.Copy(a[0], a[1], true);
        else {
            // refresh from original only the sectors we may touch: restore whole DATA.PAC + EXE regions
        }
        using (var fs = new FileStream(a[1], FileMode.Open, FileAccess.ReadWrite)) {
            using (var src = File.OpenRead(a[0])) {
                // restore DATA.PAC and EXE sector ranges from original first (so removed patches revert)
                foreach (var r in new[] { new[] { 243023L, 2450L }, new[] { 245473L, 144L } }) {
                    var buf = new byte[2352 * r[1]];
                    src.Seek(r[0] * 2352, SeekOrigin.Begin); src.Read(buf, 0, buf.Length);
                    fs.Seek(r[0] * 2352, SeekOrigin.Begin); fs.Write(buf, 0, buf.Length);
                }
            }
            WriteFile(fs, 243023, pac);
            string exe = work + @"\out\SLPS_023.24";
            if (File.Exists(exe)) WriteFile(fs, 245473, File.ReadAllBytes(exe));
        }
        Console.WriteLine("sectors changed: " + changed);
    }
}
