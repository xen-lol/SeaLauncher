using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SeaLauncher.Ui
{
    public class PromptWindow : Window
    {
        private readonly TextBox box = new TextBox();

        public string Result { get; private set; }

        public PromptWindow(string title, string label, string initial)
        {
            Title = title;
            Width = 440;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Theme.Deep;
            FontFamily = Theme.Font;

            var root = new StackPanel();
            root.Margin = new Thickness(24, 22, 24, 22);

            var heading = new TextBlock();
            heading.Text = label;
            heading.FontSize = 13;
            heading.Foreground = Theme.Muted;
            heading.Margin = new Thickness(0, 0, 0, 10);
            root.Children.Add(heading);

            var field = new Border();
            field.Background = Theme.FieldBack;
            field.BorderBrush = Theme.Edge;
            field.BorderThickness = new Thickness(1);
            field.CornerRadius = new CornerRadius(10);
            field.Height = 44;

            box.Text = initial ?? "";
            box.Background = Brushes.Transparent;
            box.BorderThickness = new Thickness(0);
            box.Foreground = Theme.Text;
            box.CaretBrush = Theme.Text;
            box.FontSize = 15;
            box.VerticalContentAlignment = VerticalAlignment.Center;
            box.Padding = new Thickness(14, 0, 14, 0);
            field.Child = box;
            root.Children.Add(field);

            var buttons = new StackPanel();
            buttons.Orientation = Orientation.Horizontal;
            buttons.HorizontalAlignment = HorizontalAlignment.Right;
            buttons.Margin = new Thickness(0, 18, 0, 0);

            ClickBorder cancel = MakeButton("Cancel", false);
            cancel.Clicked += () => { DialogResult = false; };
            buttons.Children.Add(cancel);

            ClickBorder save = MakeButton("Save", true);
            save.Margin = new Thickness(10, 0, 0, 0);
            save.Clicked += Accept;
            buttons.Children.Add(save);

            root.Children.Add(buttons);
            Content = root;

            SourceInitialized += (s, e) => WindowTools.StyleTitleBar(this);
            Loaded += (s, e) =>
            {
                box.Focus();
                box.SelectAll();
            };
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Accept();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    e.Handled = true;
                }
            };
        }

        private void Accept()
        {
            Result = box.Text.Trim();
            DialogResult = true;
        }

        private static ClickBorder MakeButton(string text, bool primary)
        {
            var button = new ClickBorder();
            button.Width = 96;
            button.Height = 38;
            button.CornerRadius = new CornerRadius(10);
            button.HoverScale = 1.03;
            button.PressScale = 0.97;

            var label = new TextBlock();
            label.Text = text;
            label.FontSize = 13.5;
            label.FontWeight = FontWeights.SemiBold;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.IsHitTestVisible = false;

            if (primary)
            {
                var back = new Border();
                back.CornerRadius = new CornerRadius(10);
                back.Background = Theme.AccentGradient;
                back.Child = label;
                label.Foreground = Theme.Deep;
                button.Child = back;
            }
            else
            {
                button.SetColors(Theme.EdgeColor, Color.FromArgb(60, 255, 255, 255));
                label.Foreground = Theme.Text;
                button.Child = label;
            }
            return button;
        }
    }
}
