using System;
using System.IO;
using System.Text;

// sysf scan <file> : find & decode system-font strings
// sysf dec <file> <start> <end>
public class Sysf {
    public static string G =
        "0123456789()?!「」" +
        "、。アイウエオカキクケコサシスセ" +
        "ソタチツテトナニヌネノハヒフヘホ" +
        "マミムメモヤユヨラリルレロワMン" +
        "ァィゥェォッャュョAガギグゲゴザ" +
        "ジズゼゾダJrデドバビブベボパピ" +
        "プペポヴーあいうえおかきくけこさ" +
        "しすせそたちつてとなにぬねのはひ" +
        "ふへほまみむめもやゆよらりるれろ" +
        "わをんぁぃぅぇぉっゃゅょゎがぎぐ" +
        "げござじずぜぞだぢづでどばびぶべ" +
        "ぼぱぴぷぺぽ□△×○ⓁⓛⓇⓡ…▼" +
        "設定中抜差押要空上書時計画面動先" +
        "攻撃武器敵落注意足場危険気力強火" +
        "近前破壊水使弱点地形戦体安全所悪" +
        "用選択ＡＢ／大小型電氷号ー＿＿＿";
    public static string Ch(int b) {
        if (b == 0xFE) return "_";
        if (b == 0) return "|";
        if (b >= 1 && b <= G.Length) return G[b - 1].ToString();
        return "{" + b.ToString("X2") + "}";
    }
    static void Main0(string[] a) {
        Console.OutputEncoding = Encoding.UTF8;
        byte[] d = File.ReadAllBytes(a[1]);
        if (a[0] == "dec") {
            int s = Convert.ToInt32(a[2], 16), e = Convert.ToInt32(a[3], 16);
            var sb = new StringBuilder();
            for (int i = s; i < e; i++) sb.Append(Ch(d[i]));
            Console.WriteLine(sb);
            return;
        }
        // scan: runs (len>=8) of bytes in kana/kanji range or FE, few others
        int o = 0;
        while (o < d.Length) {
            int st = o, good = 0, bad = 0;
            while (o < d.Length) {
                int b = d[o];
                bool ok = (b >= 0x11 && b <= 0xFE) || (b >= 1 && b <= 0x10);
                if (!ok) break;
                if (b >= 0x66 && b <= 0xB6 || b == 0xFE) good++; else bad++;
                o++;
            }
            bool term = o + 3 < d.Length && d[o]==0 && d[o+1]==0 && d[o+2]==0 && d[o+3]==0; int fe=0; for (int i = st; i < o; i++) if (d[i]==0xFE) fe++; if (term && o - st >= 4 && good * 3 >= (o - st) * 2 && ((o-st)%4==0)) {
                var sb = new StringBuilder();
                for (int i = st; i < o; i++) sb.Append(Ch(d[i]));
                Console.WriteLine("{0:X6} [{1}] {2}", st, o - st, sb);
            }
            o++;
        }
    }
}


