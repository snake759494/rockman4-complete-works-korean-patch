using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

// nes scan <rom> : find runs dense with kana codes; nes dec <rom> <starthex> <endhex>
class Nes {
    public static string[] T = new string[256];
    static Nes() {
        string kata = "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000カイカクコサスソタトナニハフホマラリルロワン";
        for (int i = 0; i < 256; i++) T[i] = "{" + i.ToString("X2") + "}";
        T[0] = " ";
        string k2 = "カイカクコサスソタトナニハフホマラリルロワン"; // 0B..20
        for (int i = 0; i < k2.Length; i++) T[0x0B + i] = k2[i].ToString();
        T[0x02] = "Dr"; T[0x04] = ".";
        string r2 = "゛゜ッュ送各地世界生守長作出▼";
        for (int i = 0; i < r2.Length; i++) T[0x21 + i] = r2[i].ToString();
        string hira = "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん゛、";
        for (int i = 0; i < hira.Length; i++) T[0x30 + i] = hira[i].ToString();
        string r4 = "っょ。・工学用平和中化天才科者自改造野望阻止秩序破壊再年ー日々";
        for (int i = 0; i < r4.Length; i++) T[0x60 + i] = r4[i].ToString();
        T[0xFD] = "\\n"; T[0xFF] = "<END>\n";
    }
    static void Main(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        byte[] d = File.ReadAllBytes(a[1]);
        if (a[0] == "scan") {
            int win = 24;
            for (int o = 0; o + win < d.Length; ) {
                int c = 0, fd = 0;
                for (int k = 0; k < win; k++) { int b = d[o + k]; if (b >= 0x30 && b <= 0x5F) c++; if (b == 0xFD || b == 0xFF) fd++; }
                if (c >= 12) { Console.WriteLine("{0:X6}", o); o += 256; } else o++;
            }
        } else {
            int s = Convert.ToInt32(a[2], 16), e = Convert.ToInt32(a[3], 16);
            var sb = new StringBuilder();
            sb.AppendFormat("@{0:X6} ", s);
            for (int i = s; i < e; i++) { sb.Append(T[d[i]]); if (d[i] == 0xFF) sb.AppendFormat("@{0:X6} ", i + 1); }
            Console.WriteLine(sb);
        }
    }
}
