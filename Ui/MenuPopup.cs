using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SeaLauncher.Ui
{
    public class MenuEntry
    {
        public string Glyph { get; set; }
        public string Text { get; set; }
        public Action Action { get; set; }
        public bool Danger { get; set; }
        public bool IsSeparator { get; set; }

        public static MenuEntry Item(string glyph, string text, Action action, bool danger)
        {
            return new MenuEntry { Glyph = glyph, Text = text, Action = action, Danger = danger };
        }

        public static MenuEntry Separator()
        {
            return new MenuEntry { IsSeparator = true };
        }
    }

    public static class MenuPopup
    {
        private const double MenuWidth = 236;
        private const double Pad = 20;

        private static Popup current;

        public static void CloseCurrent()
        {
            if (current != null)
            {
                current.IsOpen = false;
                current = null;
            }
        }

        public static void Show(FrameworkElement anchor, bool atMouse, IList<MenuEntry> entries)
        {
            CloseCurrent();

            var popup = new Popup();
            popup.AllowsTransparency = true;
            popup.StaysOpen = false;
            popup.PopupAnimation = PopupAnimation.Fade;
            popup.PlacementTarget = anchor;

            var panel = new StackPanel();
            panel.Margin = new Thickness(6);

            foreach (MenuEntry entry in entries)
            {
                if (entry.IsSeparator)
                {
                    var line = new Border();
                    line.Height = 1;
                    line.Margin = new Thickness(8, 5, 8, 5);
                    line.Background = Theme.Divider;
                    panel.Children.Add(line);
                    continue;
                }
                panel.Children.Add(BuildRow(popup, entry));
            }

            var frame = new Border();
            frame.Width = MenuWidth;
            frame.Margin = new Thickness(Pad);
            frame.CornerRadius = new CornerRadius(14);
            frame.Background = Theme.MenuBack;
            frame.BorderBrush = Theme.Edge;
            frame.BorderThickness = new Thickness(1);
            frame.Child = panel;
            frame.Effect = new DropShadowEffect
            {
                BlurRadius = 26,
                ShadowDepth = 6,
                Direction = 270,
                Opacity = 0.55,
                Color = Colors.Black
            };

            popup.Child = frame;

            if (atMouse)
            {
                popup.Placement = PlacementMode.MousePoint;
                popup.HorizontalOffset = -Pad + 4;
                popup.VerticalOffset = -Pad + 4;
            }
            else
            {
                popup.Placement = PlacementMode.Bottom;
                popup.HorizontalOffset = anchor.ActualWidth - (MenuWidth + Pad);
                popup.VerticalOffset = -Pad + 6;
            }

            popup.Closed += (s, e) =>
            {
                if (current == popup)
                {
                    current = null;
                }
            };

            current = popup;
            popup.IsOpen = true;
        }

        private static FrameworkElement BuildRow(Popup popup, MenuEntry entry)
        {
            var row = new ClickBorder();
            row.Height = 36;
            row.CornerRadius = new CornerRadius(9);
            row.SetColors(Theme.ClearColor, Theme.HoverColor);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Brush tint = entry.Danger ? Theme.Danger : Theme.Text;

            TextBlock glyph = Logos.Glyph(entry.Glyph, 14, entry.Danger ? Theme.Danger : Theme.Accent);
            Grid.SetColumn(glyph, 0);
            grid.Children.Add(glyph);

            var label = new TextBlock();
            label.Text = entry.Text;
            label.FontFamily = Theme.Font;
            label.FontSize = 13.5;
            label.Foreground = tint;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.IsHitTestVisible = false;
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            row.Child = grid;

            Action action = entry.Action;
            row.Clicked += () =>
            {
                popup.IsOpen = false;
                if (action != null)
                {
                    action();
                }
            };
            return row;
        }
    }
}
