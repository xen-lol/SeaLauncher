using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SeaLauncher.Models;
using SeaLauncher.Services;
using Shapes = System.Windows.Shapes;

namespace SeaLauncher.Ui
{
    public class GameCard : Border
    {
        public const double CardWidth = 192;
        public const double CardHeight = 288;

        private readonly Action<GameCard> onPlay;
        private readonly Action<GameCard, FrameworkElement, bool> onMenu;
        private readonly SolidColorBrush edge = new SolidColorBrush(Theme.EdgeColor);
        private readonly ScaleTransform scale = new ScaleTransform(1, 1);
        private readonly TranslateTransform lift = new TranslateTransform(0, 0);
        private readonly Image coverImage = new Image();
        private readonly Image iconImage = new Image();
        private readonly TextBlock letter = new TextBlock();
        private readonly TextBlock title = new TextBlock();
        private readonly TextBlock favoriteMark;
        private readonly Border artBack = new Border();
        private readonly ClickBorder playButton = new ClickBorder();
        private readonly ClickBorder dotsButton = new ClickBorder();
        private int artVersion;

        public Game Game { get; private set; }

        public GameCard(Game game, Action<GameCard> play, Action<GameCard, FrameworkElement, bool> menu)
        {
            Game = game;
            onPlay = play;
            onMenu = menu;

            Width = CardWidth;
            Height = CardHeight;
            Margin = new Thickness(9);
            CornerRadius = new CornerRadius(14);
            Background = Theme.CardBase;
            BorderThickness = new Thickness(1);
            BorderBrush = edge;
            RenderTransformOrigin = new Point(0.5, 0.5);

            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(lift);
            RenderTransform = group;

            double innerWidth = CardWidth - 2;
            double innerHeight = CardHeight - 2;

            var inner = new Grid();
            inner.Width = innerWidth;
            inner.Height = innerHeight;
            inner.Clip = new RectangleGeometry(new Rect(0, 0, innerWidth, innerHeight), 13, 13);

            artBack.Background = MakeGradient(game.Name);
            inner.Children.Add(artBack);

            iconImage.Width = 108;
            iconImage.Height = 108;
            iconImage.Stretch = Stretch.Uniform;
            iconImage.HorizontalAlignment = HorizontalAlignment.Center;
            iconImage.VerticalAlignment = VerticalAlignment.Center;
            iconImage.Margin = new Thickness(0, 0, 0, 70);
            iconImage.Visibility = Visibility.Collapsed;
            iconImage.IsHitTestVisible = false;
            RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.HighQuality);
            inner.Children.Add(iconImage);

            letter.FontSize = 76;
            letter.FontWeight = FontWeights.Bold;
            letter.Foreground = Theme.Solid(Color.FromArgb(70, 255, 255, 255));
            letter.HorizontalAlignment = HorizontalAlignment.Center;
            letter.VerticalAlignment = VerticalAlignment.Center;
            letter.Margin = new Thickness(0, 0, 0, 70);
            letter.Visibility = Visibility.Collapsed;
            letter.IsHitTestVisible = false;
            inner.Children.Add(letter);

            coverImage.Stretch = Stretch.UniformToFill;
            coverImage.Opacity = 0;
            coverImage.IsHitTestVisible = false;
            inner.Children.Add(coverImage);

            var shade = new Border();
            shade.Height = 176;
            shade.VerticalAlignment = VerticalAlignment.Bottom;
            shade.Background = Theme.CardShade;
            shade.IsHitTestVisible = false;
            inner.Children.Add(shade);

            favoriteMark = Logos.Glyph("\uE735", 15, Theme.Gold);
            favoriteMark.HorizontalAlignment = HorizontalAlignment.Left;
            favoriteMark.VerticalAlignment = VerticalAlignment.Top;
            favoriteMark.Margin = new Thickness(12, 12, 0, 0);
            inner.Children.Add(favoriteMark);

            BuildDots();
            dotsButton.HorizontalAlignment = HorizontalAlignment.Right;
            dotsButton.VerticalAlignment = VerticalAlignment.Top;
            dotsButton.Margin = new Thickness(0, 9, 9, 0);
            dotsButton.Opacity = 0;
            inner.Children.Add(dotsButton);

            var info = new Grid();
            info.VerticalAlignment = VerticalAlignment.Bottom;
            info.Margin = new Thickness(14, 0, 14, 14);
            info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            title.FontSize = 14.5;
            title.FontWeight = FontWeights.SemiBold;
            title.Foreground = Theme.Text;
            title.TextWrapping = TextWrapping.Wrap;
            title.TextTrimming = TextTrimming.CharacterEllipsis;
            title.LineHeight = 19;
            title.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            title.MaxHeight = 38;
            title.Margin = new Thickness(0, 0, 0, 10);
            title.IsHitTestVisible = false;
            Grid.SetRow(title, 0);
            info.Children.Add(title);

            var bottom = new Grid();
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(bottom, 1);

            FrameworkElement badge = Logos.Badge(game.Platform, 28);
            badge.HorizontalAlignment = HorizontalAlignment.Left;
            badge.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(badge, 0);
            bottom.Children.Add(badge);

            BuildPlay();
            Grid.SetColumn(playButton, 1);
            playButton.Opacity = 0;
            bottom.Children.Add(playButton);

            info.Children.Add(bottom);
            inner.Children.Add(info);

            Child = inner;

            Refresh(game);
            LoadArt();
        }

        public void Refresh(Game updated)
        {
            bool coverChanged = !string.Equals(Game.CoverPath, updated.CoverPath, StringComparison.OrdinalIgnoreCase);

            if (!ReferenceEquals(Game, updated))
            {
                Game.Name = updated.Name;
                Game.OriginalName = updated.OriginalName;
                Game.InstallDir = updated.InstallDir;
                Game.ExePath = updated.ExePath;
                Game.Arguments = updated.Arguments;
                Game.LaunchKey = updated.LaunchKey;
                Game.IconPath = updated.IconPath;
                Game.CoverPath = updated.CoverPath;
            }

            title.Text = Game.Name;
            letter.Text = FirstLetter(Game.Name);
            artBack.Background = MakeGradient(Game.OriginalName ?? Game.Name);
            SetFavorite();

            if (coverChanged)
            {
                LoadArt();
            }
        }

        public void SetFavorite()
        {
            favoriteMark.Visibility = Game.Favorite ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ReloadArt()
        {
            coverImage.Source = null;
            Anim.SetOpacity(coverImage, 0);
            iconImage.Source = null;
            iconImage.Visibility = Visibility.Collapsed;
            letter.Visibility = Visibility.Collapsed;
            LoadArt();
        }

        private void LoadArt()
        {
            artVersion++;
            int version = artVersion;
            string cover = Game.CoverPath;
            string icon = Game.IconPath;
            string exe = Game.ExePath;

            Task.Run(() =>
            {
                ImageSource coverSource = null;
                ImageSource iconSource = null;

                if (!string.IsNullOrEmpty(cover))
                {
                    coverSource = ImageTools.LoadFile(cover, 340);
                }
                if (coverSource == null)
                {
                    if (!string.IsNullOrEmpty(icon))
                    {
                        iconSource = ImageTools.LoadFile(icon, 256);
                    }
                    if (iconSource == null && !string.IsNullOrEmpty(exe))
                    {
                        iconSource = ImageTools.ExtractIcon(exe);
                    }
                }

                Dispatcher.BeginInvoke(new Action(() => ApplyArt(version, coverSource, iconSource)));
            });
        }

        private void ApplyArt(int version, ImageSource cover, ImageSource icon)
        {
            if (version != artVersion)
            {
                return;
            }

            if (cover != null)
            {
                coverImage.Source = cover;
                iconImage.Visibility = Visibility.Collapsed;
                letter.Visibility = Visibility.Collapsed;
                coverImage.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(320)));
            }
            else if (icon != null)
            {
                iconImage.Source = icon;
                iconImage.Visibility = Visibility.Visible;
                letter.Visibility = Visibility.Collapsed;
                Anim.SetOpacity(coverImage, 0);
                iconImage.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)));
            }
            else
            {
                iconImage.Visibility = Visibility.Collapsed;
                letter.Visibility = Visibility.Visible;
                Anim.SetOpacity(coverImage, 0);
            }
        }

        private void BuildDots()
        {
            dotsButton.Width = 32;
            dotsButton.Height = 32;
            dotsButton.CornerRadius = new CornerRadius(16);
            dotsButton.HoverScale = 1.08;
            dotsButton.PressScale = 0.92;
            dotsButton.SetColors(Color.FromArgb(120, 2, 10, 18), Color.FromArgb(190, 20, 60, 86));

            var group = new GeometryGroup();
            group.Children.Add(new EllipseGeometry(new Point(2, 2), 1.7, 1.7));
            group.Children.Add(new EllipseGeometry(new Point(7, 2), 1.7, 1.7));
            group.Children.Add(new EllipseGeometry(new Point(12, 2), 1.7, 1.7));

            var dots = new Shapes.Path();
            dots.Data = group;
            dots.Fill = Brushes.White;
            dots.Width = 14;
            dots.Height = 4;
            dots.HorizontalAlignment = HorizontalAlignment.Center;
            dots.VerticalAlignment = VerticalAlignment.Center;
            dots.IsHitTestVisible = false;
            dotsButton.Child = dots;

            dotsButton.Clicked += () => onMenu(this, dotsButton, false);
        }

        private void BuildPlay()
        {
            playButton.Width = 42;
            playButton.Height = 42;
            playButton.CornerRadius = new CornerRadius(21);
            playButton.HoverScale = 1.1;
            playButton.PressScale = 0.93;

            var host = new Grid();

            var disc = new Border();
            disc.CornerRadius = new CornerRadius(21);
            disc.Background = Theme.AccentGradient;
            disc.IsHitTestVisible = false;
            host.Children.Add(disc);

            var triangle = new Shapes.Path();
            triangle.Data = Geometry.Parse("M0,0 L11,6.5 L0,13 Z");
            triangle.Fill = Theme.Deep;
            triangle.Width = 11;
            triangle.Height = 13;
            triangle.Margin = new Thickness(3, 0, 0, 0);
            triangle.HorizontalAlignment = HorizontalAlignment.Center;
            triangle.VerticalAlignment = VerticalAlignment.Center;
            triangle.IsHitTestVisible = false;
            host.Children.Add(triangle);

            playButton.Child = host;
            playButton.Clicked += () => onPlay(this);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            Panel.SetZIndex(this, 5);
            Anim.To(scale, ScaleTransform.ScaleXProperty, 1.035, 170);
            Anim.To(scale, ScaleTransform.ScaleYProperty, 1.035, 170);
            Anim.To(lift, TranslateTransform.YProperty, -5, 170);
            Anim.ColorTo(edge, SolidColorBrush.ColorProperty, Theme.GlowColor, 170);
            Anim.To(playButton, UIElement.OpacityProperty, 1, 150);
            Anim.To(dotsButton, UIElement.OpacityProperty, 1, 150);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            Panel.SetZIndex(this, 0);
            Anim.To(scale, ScaleTransform.ScaleXProperty, 1.0, 190);
            Anim.To(scale, ScaleTransform.ScaleYProperty, 1.0, 190);
            Anim.To(lift, TranslateTransform.YProperty, 0, 190);
            Anim.ColorTo(edge, SolidColorBrush.ColorProperty, Theme.EdgeColor, 190);
            Anim.To(playButton, UIElement.OpacityProperty, 0, 170);
            Anim.To(dotsButton, UIElement.OpacityProperty, 0, 170);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!e.Handled && e.ClickCount == 2)
            {
                e.Handled = true;
                onPlay(this);
            }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (!e.Handled)
            {
                e.Handled = true;
                onMenu(this, this, true);
            }
        }

        private static string FirstLetter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    return char.ToUpperInvariant(c).ToString();
                }
            }
            return "?";
        }

        private static Brush MakeGradient(string name)
        {
            int hash = 17;
            foreach (char c in name ?? "")
            {
                hash = (hash * 31 + c) & 0x7fffffff;
            }
            double hue = hash % 360;
            Color top = ColorTools.FromHsl(hue, 0.55, 0.32);
            Color bottom = ColorTools.FromHsl((hue + 40) % 360, 0.6, 0.11);
            var brush = new LinearGradientBrush(top, bottom, 90);
            brush.Freeze();
            return brush;
        }
    }
}
