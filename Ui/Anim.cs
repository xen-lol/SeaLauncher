using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SeaLauncher.Ui
{
    public static class Anim
    {
        private static readonly IEasingFunction Ease = CreateEase();

        private static IEasingFunction CreateEase()
        {
            var ease = new CubicEase();
            ease.EasingMode = EasingMode.EaseOut;
            ease.Freeze();
            return ease;
        }

        public static void To(IAnimatable target, DependencyProperty property, double value, int milliseconds)
        {
            var animation = new DoubleAnimation(value, TimeSpan.FromMilliseconds(milliseconds));
            animation.EasingFunction = Ease;
            target.BeginAnimation(property, animation);
        }

        public static void ColorTo(IAnimatable target, DependencyProperty property, Color value, int milliseconds)
        {
            var animation = new ColorAnimation(value, TimeSpan.FromMilliseconds(milliseconds));
            animation.EasingFunction = Ease;
            target.BeginAnimation(property, animation);
        }

        public static void SetOpacity(UIElement element, double value)
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = value;
        }
    }
}
