using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;

// emu start <cue> | emu keys <seq> | emu shot <png> | emu stop
// seq: "Enter:100,wait:500,K:80"  (key:holdms) ; wait:ms
class Emu {
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc p, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc p, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint c, uint t);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    struct RECT { public int L, T, R, B; }

    static List<IntPtr> TopWins() {
        var res = new List<IntPtr>();
        var ps = Process.GetProcessesByName("duckstation-qt-x64-ReleaseLTCG");
        var pids = new HashSet<uint>(); foreach (var p in ps) pids.Add((uint)p.Id);
        EnumWindows((h, l) => { uint pid; GetWindowThreadProcessId(h, out pid); if (pids.Contains(pid) && IsWindowVisible(h)) res.Add(h); return true; }, IntPtr.Zero);
        return res;
    }
    static IntPtr MainWin() {
        IntPtr best = IntPtr.Zero; int area = 0;
        foreach (var h in TopWins()) { RECT r; GetWindowRect(h, out r); int a = (r.R - r.L) * (r.B - r.T); if (a > area) { area = a; best = h; } }
        return best;
    }
    static List<IntPtr> Targets() {
        var t = new List<IntPtr>();
        IntPtr m = MainWin(); if (m == IntPtr.Zero) return t;
        t.Add(m);
        EnumChildWindows(m, (h, l) => { t.Add(h); return true; }, IntPtr.Zero);
        return t;
    }
    static int Vk(string k) {
        switch (k) {
            case "Up": return 0x26; case "Down": return 0x28; case "Left": return 0x25; case "Right": return 0x27;
            case "Enter": return 0x0D; case "Back": return 0x08; case "Esc": return 0x1B; case "Space": return 0x20;
            case "F1": return 0x70; case "F2": return 0x71; case "F3": return 0x72; case "F4": return 0x73; case "F10": return 0x79;
            case "Tab": return 0x09;
        }
        return (int)Char.ToUpper(k[0]);
    }
    static void Key(int vk, bool down) {
        uint sc = MapVirtualKey((uint)vk, 0);
        bool ext = vk >= 0x25 && vk <= 0x28;
        uint l = 1 | (sc << 16) | (ext ? 1u << 24 : 0) | (down ? 0 : (3u << 30));
        foreach (var h in Targets()) PostMessage(h, down ? 0x100u : 0x101u, (IntPtr)vk, (IntPtr)(int)l);
    }
    static void Main(string[] a) {
        if (a[0] == "start") {
            IntPtr fg = GetForegroundWindow();
            var psi = new ProcessStartInfo(@"C:\Users\Jay\AppData\Local\Programs\DuckStation\duckstation-qt-x64-ReleaseLTCG.exe");
            psi.Arguments = "-batch -nofullscreen \"" + a[1] + "\"";
            psi.UseShellExecute = false;
            Process.Start(psi);
            var done = new HashSet<IntPtr>();
            for (int i = 0; i < 150; i++) {
                Thread.Sleep(50);
                foreach (var h in TopWins()) {
                    if (done.Contains(h)) continue;
                    int ex = GetWindowLong(h, -20);
                    SetWindowLong(h, -20, ex | 0x08000000 | 0x00000080); // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                    SetWindowPos(h, IntPtr.Zero, -3000, 50, 660, 540, 0x0010 | 0x0004);
                    done.Add(h);
                }
                IntPtr cur = GetForegroundWindow();
                uint pid; GetWindowThreadProcessId(cur, out pid);
                bool ours = false; foreach (var p in Process.GetProcessesByName("duckstation-qt-x64-ReleaseLTCG")) if (p.Id == pid) ours = true;
                if (ours && fg != IntPtr.Zero) SetForegroundWindow(fg);
            }
            Console.WriteLine("started " + MainWin());        } else if (a[0] == "keys") {
            foreach (var h in Targets()) { PostMessage(h, 0x001C, (IntPtr)1, IntPtr.Zero); PostMessage(h, 0x0006, (IntPtr)1, IntPtr.Zero); PostMessage(h, 0x0007, IntPtr.Zero, IntPtr.Zero); }
            Thread.Sleep(50);
            foreach (var part in a[1].Split(',')) {
                var kv = part.Split(':');
                int ms = kv.Length > 1 ? int.Parse(kv[1]) : 100;
                if (kv[0] == "wait") { Thread.Sleep(ms); continue; }
                int vk = Vk(kv[0]);
                Key(vk, true); Thread.Sleep(ms); Key(vk, false); Thread.Sleep(60);
            }
        } else if (a[0] == "shot") {
            IntPtr m = MainWin();
            RECT r; GetWindowRect(m, out r);
            int w = r.R - r.L, hh = r.B - r.T;
            using (var bmp = new Bitmap(w, hh)) {
                using (var g = Graphics.FromImage(bmp)) { IntPtr dc = g.GetHdc(); PrintWindow(m, dc, 2); g.ReleaseHdc(dc); }
                bmp.Save(a[1], ImageFormat.Png);
            }
            Console.WriteLine("shot " + w + "x" + hh);
        } else if (a[0] == "stop") {
            foreach (var p in Process.GetProcessesByName("duckstation-qt-x64-ReleaseLTCG")) { p.Kill(); }
        } else if (a[0] == "info") {
            foreach (var h in Targets()) { var sb = new StringBuilder(256); GetClassName(h, sb, 256); RECT r; GetWindowRect(h, out r); Console.WriteLine("{0} {1} {2},{3},{4},{5}", h, sb, r.L, r.T, r.R, r.B); }
        }
    }
}
