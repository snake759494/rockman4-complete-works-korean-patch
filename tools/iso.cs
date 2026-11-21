using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

// usage: iso list <bin> | iso extract <bin> <outdir>
class Iso {
    static FileStream fs;
    public static byte[] ReadSec(long lba) {
        byte[] b = new byte[2048];
        fs.Seek(lba * 2352 + 24, SeekOrigin.Begin);
        fs.Read(b, 0, 2048);
        return b;
    }
    static int U32(byte[] b, int o) { return b[o] | b[o+1]<<8 | b[o+2]<<16 | b[o+3]<<24; }
    static void Walk(long lba, int size, string path, string outdir, StreamWriter lw) {
        int nsec = (size + 2047) / 2048;
        for (int s = 0; s < nsec; s++) {
            byte[] d = ReadSec(lba + s);
            int p = 0;
            while (p < 2048) {
                int len = d[p];
                if (len == 0) break;
                int elba = U32(d, p+2), esz = U32(d, p+10);
                int flags = d[p+25];
                int nl = d[p+32];
                string name = Encoding.ASCII.GetString(d, p+33, nl);
                if (!(nl == 1 && (d[p+33] == 0 || d[p+33] == 1))) {
                    string full = path + "/" + name;
                    if ((flags & 2) != 0) {
                        lw.WriteLine("D {0} {1} {2}", elba, esz, full);
                        Walk(elba, esz, full, outdir, lw);
                    } else {
                        lw.WriteLine("F {0} {1} {2}", elba, esz, full);
                        if (outdir != null) {
                            string op = Path.Combine(outdir, full.TrimStart('/').Replace(";1", "").Replace('/', '\\'));
                            Directory.CreateDirectory(Path.GetDirectoryName(op));
                            using (var o = File.Create(op)) {
                                int left = esz; long l = elba;
                                // XA form2 check via subheader
                                while (left > 0) {
                                    byte[] raw = new byte[2352];
                                    fs.Seek(l * 2352, SeekOrigin.Begin); fs.Read(raw, 0, 2352);
                                    bool form2 = (raw[18] & 0x20) != 0;
                                    int n = form2 ? 2324 : 2048;
                                    int w = Math.Min(n, left);
                                    o.Write(raw, 24, w);
                                    left -= w; l++;
                                }
                            }
                        }
                    }
                }
                p += len;
            }
        }
    }
    static void Main(string[] a) {
        fs = File.OpenRead(a[1]);
        byte[] pvd = ReadSec(16);
        int rlba = U32(pvd, 156 + 2), rsz = U32(pvd, 156 + 10);
        using (var lw = new StreamWriter(Console.OpenStandardOutput())) {
            lw.WriteLine("ROOT {0} {1} volsize {2}", rlba, rsz, U32(pvd, 80));
            Walk(rlba, rsz, "", a[0] == "extract" ? a[2] : null, lw);
        }
    }
}
