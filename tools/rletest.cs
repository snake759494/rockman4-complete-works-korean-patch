using System; using System.IO; using System.Collections.Generic;
class RleTest { static void Main(string[] a) {
    var s = File.ReadAllBytes(a[0]);
    List<byte[]> h; List<int> offs;
    var raw = Rle.Decode(s, out h, out offs);
    File.WriteAllBytes(a[1], raw);
    for (int i = 0; i < h.Count; i++) Console.WriteLine("block {0} off {1:X} hdr {2}", i, offs[i], BitConverter.ToString(h[i]));
    var re = Rle.Encode(raw, s, h, offs);
    List<byte[]> h2; List<int> o2; var raw2 = Rle.Decode(re, out h2, out o2);
    bool same = raw2.Length == raw.Length; for (int i = 0; same && i < raw.Length; i++) if (raw[i] != raw2[i]) same = false;
    Console.WriteLine("orig {0} bytes, re-encoded {1} bytes, roundtrip {2}", s.Length, re.Length, same);
    Console.WriteLine("raw head: " + BitConverter.ToString(raw, 0, 48));
} }
