using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SeaLauncher.Models;
using Shapes = System.Windows.Shapes;

namespace SeaLauncher.Ui
{
    public static class Logos
    {
        public static FrameworkElement Badge(Platform platform, double size)
        {
            Geometry mark = MarkFor(platform);
            if (mark != null)
            {
                return RealMark(mark, platform, size);
            }

            Canvas art;
            switch (platform)
            {
                case Platform.Steam: art = Steam(); break;
                case Platform.Epic: art = Epic(); break;
                case Platform.Xbox: art = Xbox(); break;
                case Platform.Gog: art = Gog(); break;
                default: art = Imported(); break;
            }

            var box = new Viewbox();
            box.Width = size;
            box.Height = size;
            box.Child = art;
            box.IsHitTestVisible = false;
            return box;
        }

        private static Geometry MarkFor(Platform platform)
        {
            switch (platform)
            {
                case Platform.Steam: return LogoPaths.Steam;
                case Platform.Epic: return LogoPaths.Epic;
                case Platform.Xbox: return LogoPaths.Xbox;
                case Platform.Gog: return LogoPaths.Gog;
                case Platform.Ubisoft: return LogoPaths.Ubisoft;
                case Platform.Ea: return LogoPaths.Ea;
                case Platform.BattleNet: return LogoPaths.BattleNet;
                default: return null;
            }
        }

        private static FrameworkElement RealMark(Geometry mark, Platform platform, double size)
        {
            double fit = (platform == Platform.Epic || platform == Platform.Gog || platform == Platform.Ea) ? 1.0 : 0.92;

            var path = new Shapes.Path();
            path.Data = mark;
            path.Fill = Brushes.White;
            path.Stretch = Stretch.Uniform;
            path.Width = size * fit;
            path.Height = size * fit;
            path.HorizontalAlignment = HorizontalAlignment.Center;
            path.VerticalAlignment = VerticalAlignment.Center;
            path.IsHitTestVisible = false;
            return path;
        }

        public static FrameworkElement WaveMark(double size)
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            Add(canvas, new RectangleGeometry(new Rect(0, 0, 24, 24), 7, 7), Theme.AccentGradient, null, 0);
            Add(canvas, Geometry.Parse("M4 10 C7 6.8 9.5 6.8 12 10 C14.5 13.2 17 13.2 20 10"), null, Brushes.White, 1.8);
            Add(canvas, Geometry.Parse("M4 15.6 C7 12.4 9.5 12.4 12 15.6 C14.5 18.8 17 18.8 20 15.6"), null, Theme.Solid(Color.FromArgb(150, 255, 255, 255)), 1.8);

            var box = new Viewbox();
            box.Width = size;
            box.Height = size;
            box.Child = canvas;
            box.IsHitTestVisible = false;
            return box;
        }

        public static TextBlock Glyph(string glyph, double size, Brush brush)
        {
            var text = new TextBlock();
            text.Text = glyph;
            text.FontFamily = Theme.IconFont;
            text.FontSize = size;
            text.Foreground = brush;
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.TextAlignment = TextAlignment.Center;
            text.IsHitTestVisible = false;
            return text;
        }

        private static Canvas Steam()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            var back = new LinearGradientBrush(Color.FromRgb(46, 82, 112), Color.FromRgb(20, 30, 44), 90);
            back.Freeze();
            Add(canvas, new EllipseGeometry(new Point(12, 12), 12, 12), back, null, 0);
            Add(canvas, new EllipseGeometry(new Point(15.4, 9.2), 3.1, 3.1), null, Brushes.White, 1.5);
            Add(canvas, new EllipseGeometry(new Point(8.6, 15.2), 2.4, 2.4), Brushes.White, null, 0);
            Add(canvas, Geometry.Parse("M10.4 13.7 L13 11.4"), null, Brushes.White, 1.8);
            return canvas;
        }

        private static Canvas Epic()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            var back = new LinearGradientBrush(Color.FromRgb(58, 58, 62), Color.FromRgb(24, 24, 27), 90);
            back.Freeze();
            Add(canvas, new EllipseGeometry(new Point(12, 12), 12, 12), back, null, 0);
            Add(canvas, Geometry.Parse("M7.8 5.6 H16.2 V15.2 L12 18.6 L7.8 15.2 Z"), Brushes.White, null, 0);
            Add(canvas, Geometry.Parse("M10.1 8.2 H14 V9.5 H11.4 V10.6 H13.7 V11.9 H11.4 V13.1 H14 V14.4 H10.1 Z"), Theme.Solid(Color.FromRgb(30, 30, 33)), null, 0);
            return canvas;
        }

        private static Canvas Xbox()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            var back = new LinearGradientBrush(Color.FromRgb(22, 140, 22), Color.FromRgb(10, 86, 10), 90);
            back.Freeze();
            Add(canvas, new EllipseGeometry(new Point(12, 12), 12, 12), back, null, 0);
            Add(canvas, new EllipseGeometry(new Point(12, 12), 6.8, 6.8), null, Brushes.White, 1.4);
            Add(canvas, Geometry.Parse("M9 8.7 L15 15.3 M15 8.7 L9 15.3"), null, Brushes.White, 1.7);
            return canvas;
        }

        private static Canvas Gog()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            var back = new LinearGradientBrush(Color.FromRgb(150, 60, 160), Color.FromRgb(92, 28, 108), 90);
            back.Freeze();
            Add(canvas, new EllipseGeometry(new Point(12, 12), 12, 12), back, null, 0);
            Add(canvas, Geometry.Parse("M15.98 8.66 A5.2 5.2 0 1 0 17.2 12 H12.6"), null, Brushes.White, 1.9);
            return canvas;
        }

        private static Canvas Imported()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };
            var back = new LinearGradientBrush(Color.FromRgb(34, 176, 196), Color.FromRgb(22, 104, 156), 90);
            back.Freeze();
            Add(canvas, new EllipseGeometry(new Point(12, 12), 12, 12), back, null, 0);
            Add(canvas, Geometry.Parse("M12 6.8 V13.4 M9.1 10.7 L12 13.6 L14.9 10.7 M7.9 16.6 H16.1"), null, Brushes.White, 1.7);
            return canvas;
        }

        private static void Add(Canvas canvas, Geometry geometry, Brush fill, Brush stroke, double thickness)
        {
            var path = new Shapes.Path();
            path.Data = geometry;
            path.Fill = fill;
            path.Stroke = stroke;
            path.StrokeThickness = thickness;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.StrokeLineJoin = PenLineJoin.Round;
            canvas.Children.Add(path);
        }
    }
}
