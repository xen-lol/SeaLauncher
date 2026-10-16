using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SeaLauncher.Ui
{
    public class SmoothScrollViewer : ScrollViewer
    {
        public static readonly DependencyProperty AnimatedOffsetProperty = DependencyProperty.Register(
            "AnimatedOffset",
            typeof(double),
            typeof(SmoothScrollViewer),
            new PropertyMetadata(0.0, OnAnimatedOffsetChanged));

        private double target;
        private bool animating;

        public SmoothScrollViewer()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            PanningMode = PanningMode.VerticalOnly;
            Focusable = false;
        }

        private static void OnAnimatedOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (SmoothScrollViewer)d;
            viewer.ScrollToVerticalOffset((double)e.NewValue);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = true;
            if (ScrollableHeight <= 0)
            {
                return;
            }

            if (!animating)
            {
                target = VerticalOffset;
            }
            target = Math.Max(0, Math.Min(ScrollableHeight, target - e.Delta * 0.85));

            var ease = new CubicEase();
            ease.EasingMode = EasingMode.EaseOut;
            var animation = new DoubleAnimation(VerticalOffset, target, TimeSpan.FromMilliseconds(380));
            animation.EasingFunction = ease;
            animation.Completed += OnAnimationCompleted;

            animating = true;
            BeginAnimation(AnimatedOffsetProperty, animation);
        }

        private void OnAnimationCompleted(object sender, EventArgs e)
        {
            animating = false;
        }

        public void JumpToTop()
        {
            BeginAnimation(AnimatedOffsetProperty, null);
            SetValue(AnimatedOffsetProperty, 0.0);
            animating = false;
            target = 0;
            ScrollToTop();
        }
    }
}
