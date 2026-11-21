using System;
using System.Net.Sockets;
using System.Text;

// gdbw <type 2|3|4|0> <addrhex> <timeout_s> : set watch/breakpoint, continue, report PC + regs when hit
class GdbW {
    static NetworkStream s;
    static void Send(string cmd) { int sum = 0; foreach (char c in cmd) sum += c; var b = Encoding.ASCII.GetBytes("$" + cmd + "#" + (sum & 0xFF).ToString("x2")); s.Write(b, 0, b.Length); }
    static string Recv() {
        var sb = new StringBuilder(); int c;
        while ((c = s.ReadByte()) != '$') { if (c < 0) throw new Exception("eof"); }
        while ((c = s.ReadByte()) != '#') sb.Append((char)c);
        s.ReadByte(); s.ReadByte(); s.WriteByte((byte)'+'); return sb.ToString();
    }
    static uint Le(string h, int i) { uint v = 0; for (int k = 3; k >= 0; k--) v = (v << 8) | Convert.ToUInt32(h.Substring(i * 8 + k * 2, 2), 16); return v; }
    static void Main(string[] a) {
        var tc = new TcpClient("127.0.0.1", 2345); s = tc.GetStream();
        s.ReadTimeout = int.Parse(a[2]) * 1000;
        Send("?"); Console.WriteLine("stat " + Recv());
        Send("Z" + a[0] + "," + a[1] + ",4"); Console.WriteLine("set " + Recv());
        Send("c");
        try {
            string r = Recv(); Console.WriteLine("stop " + r);
            Send("g"); string g = Recv();
            string[] names = {"zero","at","v0","v1","a0","a1","a2","a3","t0","t1","t2","t3","t4","t5","t6","t7","s0","s1","s2","s3","s4","s5","s6","s7","t8","t9","k0","k1","gp","sp","fp","ra","sr","lo","hi","bad","cause","pc"};
            for (int i = 0; i < names.Length && (i + 1) * 8 <= g.Length; i++) Console.Write("{0}={1:X8} ", names[i], Le(g, i));
            Console.WriteLine();
        } catch (Exception e) { Console.WriteLine("timeout/err: " + e.Message); }
        Send("z" + a[0] + "," + a[1] + ",4"); try { Recv(); } catch { }
        Send("c");
        tc.Close();
    }
}
