using System;
using System.Drawing;
using System.Drawing.Imaging;

// grid <png> <out> <tileRowStart> <rows> : enlarge 8x8 tiles with hex code labels (code = (row-start)*32+col)
class Grid {
    static void Main(string[] a) {
        var im = new Bitmap(a[0]);
        int r0 = int.Parse(a[2]), rows = int.Parse(a[3]);
        int S = 4, cell = 8 * S + 14;
        using (var o = new Bitmap(32 * (8 * S + 4), rows * cell))
        using (var g = Graphics.FromImage(o)) {
            g.Clear(Color.Black);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            var f = new Font("Consolas", 8);
            for (int r = 0; r < rows; r++) for (int c = 0; c < 32; c++) {
                int x = c * (8 * S + 4), y = r * cell;
                g.DrawImage(im, new Rectangle(x, y + 12, 8 * S, 8 * S), new Rectangle(c * 8, (r0 + r) * 8 + int.Parse(a[4]), 8, 8), GraphicsUnit.Pixel);
                g.DrawString((r * 32 + c).ToString("X2"), f, Brushes.Yellow, x, y);
            }
            o.Save(a[1], ImageFormat.Png);
        }
    }
}

