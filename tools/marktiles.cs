using System; using System.IO;
// marktiles <02.bin in> <out> : write hex labels into 8x8 cells at x=4+8k, y=36+8r (r 0..8, k 0..30)
class Mark { static void Main(string[] a) {
    var d = File.ReadAllBytes(a[0]);
    int pix = 0x04022C + 8 + 44 + 12;
    string[] F = { "111101101101111","010110010010111","111001111100111","111001111001111","101101111001001","111100111001111","111100111101111","111001001001001","111101111101111","111101111001111","111101111101101","110101110101110","111100100100111","110101101101110","111100111100111","111100111100100" };
    Action<int,int,int> set = (x, y, v) => { int o = pix + y * 128 + x / 2; d[o] = (byte)((x & 1) == 0 ? (d[o] & 0xF0) | v : (d[o] & 0x0F) | (v << 4)); };
    for (int r = 0; r <= 8; r++) for (int k = 0; k < 31; k++) {
        int n = r * 31 + k; int cx = 4 + 8 * k, cy = 36 + 8 * r;
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) set(cx + x, cy + y, 0);
        int hi = (n >> 4) & 15, lo = n & 15;
        for (int y = 0; y < 5; y++) for (int x = 0; x < 3; x++) {
            if (F[hi][y * 3 + x] == '1') set(cx + x, cy + 1 + y, 1);
            if (F[lo][y * 3 + x] == '1') set(cx + 4 + x, cy + 1 + y, 1);
        }
    }
    File.WriteAllBytes(a[1], d);
} }

