using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SeaLauncher.Models;

namespace SeaLauncher.Ui
{
    public class NavItem : ClickBorder
    {
        private static readonly Brush Idle = Theme.Solid(Color.FromRgb(178, 212, 228));

        private readonly TextBlock label = new TextBlock();
        private readonly TextBlock count = new TextBlock();
        private readonly Border bar = new Border();

        public NavFilter Filter { get; private set; }

        public NavItem(NavFilter filter, string text, FrameworkElement icon)
        {
            Filter = filter;
            Height = 38;
            Margin = new Thickness(12, 1, 12, 1);
            CornerRadius = new CornerRadius(10);
            SetColors(Theme.ClearColor, Theme.HoverColor);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            label.Text = text;
            label.FontSize = 14;
            label.Foreground = Idle;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.IsHitTestVisible = false;
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            count.FontSize = 12;
            count.Foreground = Theme.Muted;
            count.Margin = new Thickness(0, 0, 14, 0);
            count.VerticalAlignment = VerticalAlignment.Center;
            count.IsHitTestVisible = false;
            Grid.SetColumn(count, 2);
            grid.Children.Add(count);

            bar.Width = 3;
            bar.Height = 18;
            bar.CornerRadius = new CornerRadius(2);
            bar.Background = Theme.Accent;
            bar.HorizontalAlignment = HorizontalAlignment.Left;
            bar.VerticalAlignment = VerticalAlignment.Center;
            bar.Visibility = Visibility.Collapsed;
            bar.IsHitTestVisible = false;
            Grid.SetColumnSpan(bar, 3);
            grid.Children.Add(bar);

            Child = grid;
        }

        public void SetSelected(bool selected)
        {
            bar.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            label.Foreground = selected ? Theme.Text : Idle;
            label.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
            if (selected)
            {
                SetColors(Theme.SelectedColor, Theme.SelectedHoverColor);
            }
            else
            {
                SetColors(Theme.ClearColor, Theme.HoverColor);
            }
        }

        public void SetCount(int value)
        {
            count.Text = value > 0 ? value.ToString() : "";
        }
    }
}
