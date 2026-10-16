using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace SeaLauncher.Ui
{
    public class SeaBackground : FrameworkElement
    {
        private class Layer
        {
            public double Base;
            public double Amp1;
            public double Amp2;
            public double Len1;
            public double Len2;
            public double Speed1;
            public double Speed2;
            public double Phase;
            public Brush Fill;
            public Pen Crest;
        }

        private readonly DrawingVisual visual = new DrawingVisual();
        private readonly List<Layer> layers = new List<Layer>();
        private readonly List<Point> points = new List<Point>();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Brush sky;
        private readonly Brush rayBrush;
        private readonly Brush bubbleFill;
        private readonly Pen bubblePen;
        private readonly double[] bubbleX = new double[28];
        private readonly double[] bubbleSize = new double[28];
        private readonly double[] bubbleSpeed = new double[28];
        private readonly double[] bubblePhase = new double[28];
        private bool running;
        private Window window;

        public SeaBackground()
        {
            IsHitTestVisible = false;
            AddVisualChild(visual);

            var skyBrush = new LinearGradientBrush();
            skyBrush.StartPoint = new Point(0, 0);
            skyBrush.EndPoint = new Point(0, 1);
            skyBrush.GradientStops.Add(new GradientStop(Color.FromRgb(7, 52, 84), 0.0));
            skyBrush.GradientStops.Add(new GradientStop(Color.FromRgb(4, 31, 54), 0.45));
            skyBrush.GradientStops.Add(new GradientStop(Color.FromRgb(2, 12, 24), 1.0));
            skyBrush.Freeze();
            sky = skyBrush;

            var ray = new LinearGradientBrush(Color.FromArgb(38, 170, 238, 255), Color.FromArgb(0, 170, 238, 255), 90);
            ray.Freeze();
            rayBrush = ray;

            var bubble = new SolidColorBrush(Color.FromArgb(26, 200, 240, 255));
            bubble.Freeze();
            bubbleFill = bubble;

            var bubbleStroke = new SolidColorBrush(Color.FromArgb(120, 200, 240, 255));
            bubbleStroke.Freeze();
            bubblePen = new Pen(bubbleStroke, 1.0);
            bubblePen.Freeze();

            AddLayer(0.30, 16, 7, 760, 330, 0.55, 0.90, 0.0, Color.FromRgb(20, 120, 160), 46, 40);
            AddLayer(0.46, 20, 8, 640, 290, 0.70, 1.10, 1.3, Color.FromRgb(28, 150, 190), 40, 34);
            AddLayer(0.60, 24, 10, 900, 400, 0.45, 0.80, 2.4, Color.FromRgb(8, 86, 128), 62, 30);
            AddLayer(0.74, 18, 9, 540, 260, 0.90, 1.30, 3.5, Color.FromRgb(36, 170, 200), 30, 34);
            AddLayer(0.88, 14, 6, 700, 320, 0.60, 1.00, 4.6, Color.FromRgb(4, 50, 92), 84, 22);

            var random = new Random(11);
            for (int i = 0; i < bubbleX.Length; i++)
            {
                bubbleX[i] = random.NextDouble();
                bubbleSize[i] = 1.6 + random.NextDouble() * 3.6;
                bubbleSpeed[i] = 0.018 + random.NextDouble() * 0.032;
                bubblePhase[i] = random.NextDouble();
            }

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visual;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            Redraw();
        }

        private void AddLayer(double baseLine, double amp1, double amp2, double len1, double len2, double speed1, double speed2, double phase, Color color, byte alpha, byte crestAlpha)
        {
            var fill = new LinearGradientBrush(
                Color.FromArgb(alpha, color.R, color.G, color.B),
                Color.FromArgb((byte)(alpha * 0.12), color.R, color.G, color.B),
                90);
            fill.Freeze();

            var crestBrush = new SolidColorBrush(Color.FromArgb(crestAlpha, 210, 245, 255));
            crestBrush.Freeze();
            var crest = new Pen(crestBrush, 1.4);
            crest.LineJoin = PenLineJoin.Round;
            crest.Freeze();

            var layer = new Layer();
            layer.Base = baseLine;
            layer.Amp1 = amp1;
            layer.Amp2 = amp2;
            layer.Len1 = len1;
            layer.Len2 = len2;
            layer.Speed1 = speed1;
            layer.Speed2 = speed2;
            layer.Phase = phase;
            layer.Fill = fill;
            layer.Crest = crest;
            layers.Add(layer);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            window = Window.GetWindow(this);
            if (!running)
            {
                CompositionTarget.Rendering += OnFrame;
                running = true;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                CompositionTarget.Rendering -= OnFrame;
                running = false;
            }
        }

        private void OnFrame(object sender, EventArgs e)
        {
            if (window != null && window.WindowState == WindowState.Minimized)
            {
                return;
            }
            Redraw();
        }

        private void Redraw()
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 2 || h < 2)
            {
                return;
            }

            double t = clock.Elapsed.TotalSeconds;
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(sky, null, new Rect(0, 0, w, h));
                DrawRays(dc, w, h, t);
                foreach (Layer layer in layers)
                {
                    DrawLayer(dc, layer, w, h, t);
                }
                DrawBubbles(dc, w, h, t);
            }
        }

        private void DrawRays(DrawingContext dc, double w, double h, double t)
        {
            double[] widths = { 0.05, 0.085, 0.04, 0.07 };
            for (int i = 0; i < widths.Length; i++)
            {
                double cx = w * (0.14 + 0.24 * i) + Math.Sin(t * 0.13 + i * 1.9) * w * 0.05;
                double top = w * widths[i];
                double skew = w * 0.09 + Math.Sin(t * 0.09 + i) * w * 0.02;
                double bottom = h * 0.9;

                var geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(cx, 0), true, true);
                    ctx.LineTo(new Point(cx + top, 0), false, false);
                    ctx.LineTo(new Point(cx + top * 1.7 + skew, bottom), false, false);
                    ctx.LineTo(new Point(cx + skew, bottom), false, false);
                }

                dc.PushOpacity(0.55 + 0.45 * Math.Sin(t * 0.35 + i * 1.3));
                dc.DrawGeometry(rayBrush, null, geometry);
                dc.Pop();
            }
        }

        private void DrawLayer(DrawingContext dc, Layer layer, double w, double h, double t)
        {
            const double step = 14.0;
            int count = (int)(w / step) + 3;
            double baseY = layer.Base * h;

            points.Clear();
            for (int i = 0; i < count; i++)
            {
                double x = i * step;
                double y = baseY
                    + layer.Amp1 * Math.Sin(x / layer.Len1 * 2.0 * Math.PI + t * layer.Speed1 + layer.Phase)
                    + layer.Amp2 * Math.Sin(x / layer.Len2 * 2.0 * Math.PI - t * layer.Speed2 + layer.Phase * 1.7);
                points.Add(new Point(x, y));
            }

            var fill = new StreamGeometry();
            using (StreamGeometryContext ctx = fill.Open())
            {
                ctx.BeginFigure(new Point(0, h + 4), true, true);
                ctx.PolyLineTo(points, false, false);
                ctx.LineTo(new Point(points[points.Count - 1].X, h + 4), false, false);
            }
            dc.DrawGeometry(layer.Fill, null, fill);

            var crest = new StreamGeometry();
            using (StreamGeometryContext ctx = crest.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                ctx.PolyLineTo(points, true, true);
            }
            dc.DrawGeometry(null, layer.Crest, crest);
        }

        private void DrawBubbles(DrawingContext dc, double w, double h, double t)
        {
            for (int i = 0; i < bubbleX.Length; i++)
            {
                double p = (t * bubbleSpeed[i] + bubblePhase[i]) % 1.0;
                double y = h * (1.04 - p * 1.08);
                double x = bubbleX[i] * w + Math.Sin(t * 0.7 + bubblePhase[i] * 25.0) * 12.0;
                double fade = Math.Sin(p * Math.PI);

                dc.PushOpacity(0.6 * fade);
                dc.DrawEllipse(bubbleFill, bubblePen, new Point(x, y), bubbleSize[i], bubbleSize[i]);
                dc.Pop();
            }
        }
    }
}
