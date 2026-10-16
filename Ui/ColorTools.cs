using System;
using System.Windows.Media;

namespace SeaLauncher.Ui
{
    public static class ColorTools
    {
        public static Color FromHsl(double hue, double saturation, double lightness)
        {
            double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            double x = chroma * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            double m = lightness - chroma / 2;
            double r = 0, g = 0, b = 0;

            if (hue < 60) { r = chroma; g = x; }
            else if (hue < 120) { r = x; g = chroma; }
            else if (hue < 180) { g = chroma; b = x; }
            else if (hue < 240) { g = x; b = chroma; }
            else if (hue < 300) { r = x; b = chroma; }
            else { r = chroma; b = x; }

            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }
    }
}
