using System;
using System.Collections.Generic;

// tiny MIPS assembler helpers
public static class M {
    static readonly Dictionary<string, int> Reg = new Dictionary<string, int>();
    static M() {
        string[] r = {"zero","at","v0","v1","a0","a1","a2","a3","t0","t1","t2","t3","t4","t5","t6","t7",
            "s0","s1","s2","s3","s4","s5","s6","s7","t8","t9","k0","k1","gp","sp","fp","ra"};
        for (int i = 0; i < 32; i++) Reg[r[i]] = i;
    }
    static uint R(string n) { return (uint)Reg[n]; }
    static uint I(uint op, string rs, string rt, int imm) { return (op << 26) | (R(rs) << 21) | (R(rt) << 16) | ((uint)imm & 0xFFFF); }
    static uint Sp(string rs, string rt, string rd, uint sa, uint fn) { return (R(rs) << 21) | (R(rt) << 16) | (R(rd) << 11) | (sa << 6) | fn; }
    public static uint nop() { return 0; }
    public static uint addiu(string rt, string rs, int imm) { return I(9, rs, rt, imm); }
    public static uint sltiu(string rt, string rs, int imm) { return I(11, rs, rt, imm); }
    public static uint slti(string rt, string rs, int imm) { return I(10, rs, rt, imm); }
    public static uint andi(string rt, string rs, int imm) { return I(12, rs, rt, imm); }
    public static uint ori(string rt, string rs, int imm) { return I(13, rs, rt, imm); }
    public static uint lui(string rt, int imm) { return I(15, "zero", rt, imm); }
    public static uint lbu(string rt, int off, string rs) { return I(36, rs, rt, off); }
    public static uint lhu(string rt, int off, string rs) { return I(37, rs, rt, off); }
    public static uint lw(string rt, int off, string rs) { return I(35, rs, rt, off); }
    public static uint sh(string rt, int off, string rs) { return I(41, rs, rt, off); }
    public static uint sw(string rt, int off, string rs) { return I(43, rs, rt, off); }
    public static uint sll(string rd, string rt, uint sa) { return Sp("zero", rt, rd, sa, 0); }
    public static uint srl(string rd, string rt, uint sa) { return Sp("zero", rt, rd, sa, 2); }
    public static uint sra(string rd, string rt, uint sa) { return Sp("zero", rt, rd, sa, 3); }
    public static uint addu(string rd, string rs, string rt) { return Sp(rs, rt, rd, 0, 33); }
    public static uint div(string rs, string rt) { return Sp(rs, rt, "zero", 0, 26); }
    public static uint mflo(string rd) { return Sp("zero", "zero", rd, 0, 18); }
    public static uint mfhi(string rd) { return Sp("zero", "zero", rd, 0, 16); }
    public static uint beq(string rs, string rt, uint pc, uint target) { return I(4, rs, rt, (int)((target - pc - 4) >> 2)); }
    public static uint bne(string rs, string rt, uint pc, uint target) { return I(5, rs, rt, (int)((target - pc - 4) >> 2)); }
    public static uint j(uint target) { return (2u << 26) | ((target >> 2) & 0x3FFFFFF); }
}
