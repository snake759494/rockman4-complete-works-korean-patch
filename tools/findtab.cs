using System; using System.IO;
// findtab <files...> : search byte/halfword tables matching observed code->slot relations
class FindTab { static void Main(string[] a) {
    int[] codes = { 0x02, 0x04, 0x0C, 0x14, 0x19, 0x1B, 0x1E, 0x23, 0x31, 0x38, 0x42, 0x45, 0x48, 0x55, 0x5D, 0x5E, 0x5F, 0x60, 0x64, 0x65 };
    int[] slots = { 31, 33, 41, 49, 54, 56, 59, 64, 78, 85, 95, 98, 101, 114, 121, 122, 123, 124, 128, 129 };
    foreach (var f in a) {
        var d = File.ReadAllBytes(f);
        for (int stride = 1; stride <= 4; stride *= 2)
            for (int o = 0; o + 0x66 * stride <= d.Length; o++) {
                int v0 = stride == 1 ? d[o + codes[0]] : BitConverter.ToUInt16(d, o + codes[0] * stride);
                int k = -1; bool ok = true; int mul = 0;
                for (int i = 1; i < codes.Length && ok; i++) {
                    int v = stride == 1 ? d[o + codes[i] * stride] : stride == 2 ? BitConverter.ToUInt16(d, o + codes[i] * stride) : BitConverter.ToInt32(d, o + codes[i] * stride);
                    int dv = v - v0, ds = slots[i] - slots[0];
                    if (mul == 0) { if (ds == 0 || dv % ds != 0 || dv == 0) { ok = false; break; } mul = dv / ds; }
                    else if (dv != ds * mul) ok = false;
                }
                if (ok) Console.WriteLine("{0} @{1:X} stride {2} mul {3}", Path.GetFileName(f), o, stride, mul);
            }
    }
} }
