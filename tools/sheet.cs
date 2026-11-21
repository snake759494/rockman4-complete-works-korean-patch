using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;

// sheet <out.png> <scale> <files...>  (glob prefix allowed via dir + prefix)
class Sheet {
    static void Main(string[] a) {
        string dir = a[2], pre = a[3];
        int sc = int.Parse(a[1]);
        var files = Directory.GetFiles(dir, pre + "*.png").OrderBy(q => q).ToArray();
        int maxW = 1400, x = 0, y = 0, rowH = 0;
        var pos = new System.Collections.Generic.List<Tuple<string, Rectangle>>();
        foreach (var f in files) {
            using (var im = Image.FromFile(f)) {
                int w = Math.Max(im.Width * sc, 90), h = im.Height * sc + 14;
                if (x + w > maxW) { x = 0; y += rowH + 4; rowH = 0; }
                pos.Add(Tuple.Create(f, new Rectangle(x, y, im.Width * sc, im.Height * sc)));
                x += w + 4; rowH = Math.Max(rowH, h);
            }
        }
        using (var bmp = new Bitmap(maxW, y + rowH + 4))
        using (var g = Graphics.FromImage(bmp)) {
            g.Clear(Color.FromArgb(255, 60, 0, 60));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            var font = new Font("Arial", 8);
            foreach (var p in pos) {
                using (var im = Image.FromFile(p.Item1)) g.DrawImage(im, p.Item2);
                g.DrawString(Path.GetFileNameWithoutExtension(p.Item1), font, Brushes.Yellow, p.Item2.X, p.Item2.Bottom);
            }
            bmp.Save(a[0], ImageFormat.Png);
        }
    }
}
