using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

// gdb dump <addrhex> <lenhex> <out> | gdb write <addrhex> <hexbytes> | gdb cont
class Gdb {
    static NetworkStream s;
    static void Send(string cmd) {
        int sum = 0; foreach (char c in cmd) sum += c;
        byte[] b = Encoding.ASCII.GetBytes("$" + cmd + "#" + (sum & 0xFF).ToString("x2"));
        s.Write(b, 0, b.Length);
    }
    static string Recv() {
        var sb = new StringBuilder();
        int c;
        while ((c = s.ReadByte()) != '$') { if (c < 0) throw new Exception("eof"); }
        while ((c = s.ReadByte()) != '#') sb.Append((char)c);
        s.ReadByte(); s.ReadByte();
        s.WriteByte((byte)'+');
        return sb.ToString();
    }
    static void Main(string[] a) {
        var tc = new TcpClient("127.0.0.1", 2345);
        s = tc.GetStream();
        s.ReadTimeout = 10000;
        if (a[0] == "dump") {
            uint addr = Convert.ToUInt32(a[1], 16); int len = Convert.ToInt32(a[2], 16);
            using (var o = File.Create(a[3])) {
                for (int p = 0; p < len; p += 0x400) {
                    int n = Math.Min(0x400, len - p);
                    Send("m" + (addr + p).ToString("x") + "," + n.ToString("x"));
                    string r = Recv();
                    if (r.StartsWith("E")) { Console.WriteLine("err " + r); return; }
                    for (int i = 0; i + 1 < r.Length; i += 2) o.WriteByte(Convert.ToByte(r.Substring(i, 2), 16));
                }
            }
        } else if (a[0] == "write") {
            Send("M" + a[1] + "," + (a[2].Length / 2).ToString("x") + ":" + a[2]); Console.WriteLine(Recv());
        }
        Send("c");
        tc.Close();
    }
}
