using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Odjezdy;

static class TrayIcons
{
    public static Icon Create(int? minutes)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            bool ready = minutes is not null;
            Color fill = ready
                ? Color.FromArgb(255, 0, 120, 215)
                : Color.FromArgb(255, 112, 112, 112);

            var circle = new RectangleF(1.5f, 1.5f, size - 4f, size - 4f);
            using (var brush = new SolidBrush(fill))
                g.FillEllipse(brush, circle);

            string text = ready
                ? DepartureClock.IconMinutes(minutes!.Value).ToString()
                : "—";

            float em = text.Length >= 2 ? 11.5f : 15f;
            using var font = new Font("Segoe UI", em, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(Color.White);
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(text, font, textBrush, new RectangleF(0, 1, size, size), format);
        }

        return BitmapToIcon(bmp);
    }

    private static Icon BitmapToIcon(Bitmap bmp)
    {
        IntPtr handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeUser32.DestroyIcon(handle);
        }
    }
}

static class NativeUser32
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
