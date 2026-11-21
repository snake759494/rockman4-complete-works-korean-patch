using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

// pat <pattern(utf8)> <files...> : find byte runs with same equality structure (bijective)
class Pat {
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        string p = a[0]; string ord = Environment.GetEnvironmentVariable("ORD");
        var pc = new List<string>();
        var si = System.Globalization.StringInfo.GetTextElementEnumerator(p);
        foreach (char c in p) pc.Add(c.ToString());
        int n = pc.Count;
        for (int fi = 1; fi < a.Length; fi++) {
            byte[] d = File.ReadAllBytes(a[fi]);
            for (int o = 0; o + n <= d.Length; o++) {
                var m = new Dictionary<string, byte>(); var r = new Dictionary<byte, string>();
                bool ok = true;
                for (int k = 0; k < n && ok; k++) {
                    byte b = d[o + k]; string c = pc[k]; if (c == "?") continue;
                    byte eb; string ec;
                    if (m.TryGetValue(c, out eb)) { if (eb != b) ok = false; }
                    else if (r.TryGetValue(b, out ec)) { ok = false; }
                    else { m[c] = b; r[b] = c; }
                }
                if (ok && ord != null) { var lst = new List<int>(); foreach (char oc in ord) { byte v; if (m.TryGetValue(oc.ToString(), out v)) lst.Add(v); } for (int z = 1; z < lst.Count; z++) if (lst[z] <= lst[z-1]) ok = false; }
                if (ok) {
                    var sb = new StringBuilder();
                    foreach (var kv in m) sb.Append(kv.Key + "=" + kv.Value.ToString("X2") + " ");
                    Console.WriteLine("{0} @{1:X}: {2}", Path.GetFileName(a[fi]), o, sb);
                }
            }
        }
    }
}

