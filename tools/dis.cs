using System;
using System.IO;
using System.Text;

// dis <file> <fileoff_hex> <count> <vaddr_of_fileoff_hex>
public class Dis {
    static string[] R = {"zero","at","v0","v1","a0","a1","a2","a3","t0","t1","t2","t3","t4","t5","t6","t7",
        "s0","s1","s2","s3","s4","s5","s6","s7","t8","t9","k0","k1","gp","sp","fp","ra"};
    public static string D(uint w, uint pc) {
        uint op = w >> 26, rs = (w >> 21) & 31, rt = (w >> 16) & 31, rd = (w >> 11) & 31, sa = (w >> 6) & 31, fn = w & 63;
        short imm = (short)(w & 0xFFFF); uint uimm = w & 0xFFFF;
        string s = R[rs], t = R[rt], d = R[rd];
        switch (op) {
            case 0:
                switch (fn) {
                    case 0: return w == 0 ? "nop" : "sll " + d + "," + t + "," + sa;
                    case 2: return "srl " + d + "," + t + "," + sa;
                    case 3: return "sra " + d + "," + t + "," + sa;
                    case 4: return "sllv " + d + "," + t + "," + s;
                    case 6: return "srlv " + d + "," + t + "," + s;
                    case 7: return "srav " + d + "," + t + "," + s;
                    case 8: return "jr " + s;
                    case 9: return "jalr " + d + "," + s;
                    case 12: return "syscall";
                    case 16: return "mfhi " + d; case 18: return "mflo " + d;
                    case 24: return "mult " + s + "," + t; case 25: return "multu " + s + "," + t;
                    case 26: return "div " + s + "," + t; case 27: return "divu " + s + "," + t;
                    case 32: return "add " + d + "," + s + "," + t; case 33: return "addu " + d + "," + s + "," + t;
                    case 34: return "sub " + d + "," + s + "," + t; case 35: return "subu " + d + "," + s + "," + t;
                    case 36: return "and " + d + "," + s + "," + t; case 37: return "or " + d + "," + s + "," + t;
                    case 38: return "xor " + d + "," + s + "," + t; case 39: return "nor " + d + "," + s + "," + t;
                    case 42: return "slt " + d + "," + s + "," + t; case 43: return "sltu " + d + "," + s + "," + t;
                }
                return "special?" + fn;
            case 1: {
                string n = rt == 0 ? "bltz" : rt == 1 ? "bgez" : rt == 16 ? "bltzal" : rt == 17 ? "bgezal" : "regimm?";
                return n + " " + s + ",0x" + (pc + 4 + (uint)(imm << 2)).ToString("X8");
            }
            case 2: return "j 0x" + (((pc + 4) & 0xF0000000) | ((w & 0x3FFFFFF) << 2)).ToString("X8");
            case 3: return "jal 0x" + (((pc + 4) & 0xF0000000) | ((w & 0x3FFFFFF) << 2)).ToString("X8");
            case 4: return "beq " + s + "," + t + ",0x" + (pc + 4 + (uint)(imm << 2)).ToString("X8");
            case 5: return "bne " + s + "," + t + ",0x" + (pc + 4 + (uint)(imm << 2)).ToString("X8");
            case 6: return "blez " + s + ",0x" + (pc + 4 + (uint)(imm << 2)).ToString("X8");
            case 7: return "bgtz " + s + ",0x" + (pc + 4 + (uint)(imm << 2)).ToString("X8");
            case 8: return "addi " + t + "," + s + "," + imm;
            case 9: return "addiu " + t + "," + s + "," + imm;
            case 10: return "slti " + t + "," + s + "," + imm;
            case 11: return "sltiu " + t + "," + s + "," + imm;
            case 12: return "andi " + t + "," + s + ",0x" + uimm.ToString("X");
            case 13: return "ori " + t + "," + s + ",0x" + uimm.ToString("X");
            case 14: return "xori " + t + "," + s + ",0x" + uimm.ToString("X");
            case 15: return "lui " + t + ",0x" + uimm.ToString("X");
            case 16: return "cop0 " + w.ToString("X8");
            case 18: return "cop2 " + w.ToString("X8");
            case 32: return "lb " + t + "," + imm + "(" + s + ")";
            case 33: return "lh " + t + "," + imm + "(" + s + ")";
            case 34: return "lwl " + t + "," + imm + "(" + s + ")";
            case 35: return "lw " + t + "," + imm + "(" + s + ")";
            case 36: return "lbu " + t + "," + imm + "(" + s + ")";
            case 37: return "lhu " + t + "," + imm + "(" + s + ")";
            case 38: return "lwr " + t + "," + imm + "(" + s + ")";
            case 40: return "sb " + t + "," + imm + "(" + s + ")";
            case 41: return "sh " + t + "," + imm + "(" + s + ")";
            case 42: return "swl " + t + "," + imm + "(" + s + ")";
            case 43: return "sw " + t + "," + imm + "(" + s + ")";
            case 46: return "swr " + t + "," + imm + "(" + s + ")";
            case 50: return "lwc2 " + rt + "," + imm + "(" + s + ")";
            case 58: return "swc2 " + rt + "," + imm + "(" + s + ")";
        }
        return ".word 0x" + w.ToString("X8");
    }
    public static void Main(string[] a) {
        byte[] f = File.ReadAllBytes(a[0]);
        int off = Convert.ToInt32(a[1], 16), n = int.Parse(a[2]);
        uint va = Convert.ToUInt32(a[3], 16);
        var sb = new StringBuilder();
        for (int i = 0; i < n && off + i * 4 + 3 < f.Length; i++) {
            uint w = BitConverter.ToUInt32(f, off + i * 4);
            uint pc = va + (uint)i * 4;
            sb.AppendLine(pc.ToString("X8") + ": " + w.ToString("X8") + "  " + D(w, pc));
        }
        Console.Write(sb);
    }
}
