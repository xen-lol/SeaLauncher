using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SeaLauncher.Ui
{
    public class ClickBorder : Border
    {
        private readonly SolidColorBrush fill = new SolidColorBrush(Colors.Transparent);
        private readonly ScaleTransform scale = new ScaleTransform(1, 1);
        private Color normal = Colors.Transparent;
        private Color hover = Colors.Transparent;
        private bool pressed;

        public event Action Clicked;

        public double HoverScale { get; set; } = 1.0;
        public double PressScale { get; set; } = 1.0;

        public ClickBorder()
        {
            Background = fill;
            Cursor = Cursors.Hand;
            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = scale;
        }

        public void SetColors(Color normalColor, Color hoverColor)
        {
            normal = normalColor;
            hover = hoverColor;
            fill.BeginAnimation(SolidColorBrush.ColorProperty, null);
            fill.Color = IsMouseOver ? hover : normal;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            Anim.ColorTo(fill, SolidColorBrush.ColorProperty, hover, 130);
            ApplyScale(HoverScale, 150);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            pressed = false;
            Anim.ColorTo(fill, SolidColorBrush.ColorProperty, normal, 170);
            ApplyScale(1.0, 170);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            pressed = true;
            e.Handled = true;
            ApplyScale(PressScale, 90);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            bool wasPressed = pressed;
            pressed = false;
            e.Handled = true;
            ApplyScale(IsMouseOver ? HoverScale : 1.0, 120);
            if (wasPressed && Clicked != null)
            {
                Clicked();
            }
        }

        private void ApplyScale(double target, int milliseconds)
        {
            if (HoverScale == 1.0 && PressScale == 1.0)
            {
                return;
            }
            Anim.To(scale, ScaleTransform.ScaleXProperty, target, milliseconds);
            Anim.To(scale, ScaleTransform.ScaleYProperty, target, milliseconds);
        }
    }
}
