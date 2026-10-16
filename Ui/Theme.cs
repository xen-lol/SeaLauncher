using System.Windows.Media;

namespace SeaLauncher.Ui
{
    public static class Theme
    {
        public static readonly FontFamily Font = new FontFamily("Segoe UI Variable Display, Segoe UI");
        public static readonly FontFamily IconFont = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        public static readonly Color DeepColor = Color.FromRgb(3, 13, 24);
        public static readonly Color AccentColor = Color.FromRgb(45, 226, 213);
        public static readonly Color AccentBlueColor = Color.FromRgb(41, 160, 240);
        public static readonly Color GlowColor = Color.FromArgb(215, 60, 220, 230);
        public static readonly Color EdgeColor = Color.FromArgb(30, 255, 255, 255);
        public static readonly Color ClearColor = Color.FromArgb(0, 255, 255, 255);
        public static readonly Color HoverColor = Color.FromArgb(24, 255, 255, 255);
        public static readonly Color SelectedColor = Color.FromArgb(40, 120, 230, 240);
        public static readonly Color SelectedHoverColor = Color.FromArgb(56, 120, 230, 240);

        public static readonly Brush Deep = Solid(DeepColor);
        public static readonly Brush Text = Solid(Color.FromRgb(232, 246, 252));
        public static readonly Brush Muted = Solid(Color.FromRgb(135, 172, 192));
        public static readonly Brush Accent = Solid(AccentColor);
        public static readonly Brush Gold = Solid(Color.FromRgb(255, 205, 90));
        public static readonly Brush Danger = Solid(Color.FromRgb(255, 125, 125));
        public static readonly Brush Divider = Solid(Color.FromArgb(26, 255, 255, 255));
        public static readonly Brush Panel = Solid(Color.FromArgb(176, 4, 17, 31));
        public static readonly Brush FieldBack = Solid(Color.FromArgb(120, 6, 26, 44));
        public static readonly Brush MenuBack = Solid(Color.FromArgb(250, 10, 28, 46));
        public static readonly Brush CardBase = Solid(Color.FromRgb(8, 26, 42));
        public static readonly Brush ScrimBack = Solid(Color.FromArgb(120, 0, 0, 0));
        public static readonly Brush Edge = Solid(EdgeColor);
        public static readonly Brush AccentGradient = MakeAccentGradient();
        public static readonly Brush CardShade = MakeCardShade();

        public static SolidColorBrush Solid(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Brush MakeAccentGradient()
        {
            var brush = new LinearGradientBrush(AccentColor, AccentBlueColor, 45);
            brush.Freeze();
            return brush;
        }

        private static Brush MakeCardShade()
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new System.Windows.Point(0, 0);
            brush.EndPoint = new System.Windows.Point(0, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 2, 10, 18), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(150, 2, 10, 18), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(240, 2, 10, 18), 1.0));
            brush.Freeze();
            return brush;
        }
    }
}
