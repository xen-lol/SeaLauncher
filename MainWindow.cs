using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using SeaLauncher.Models;
using SeaLauncher.Services;
using SeaLauncher.Ui;
using Shapes = System.Windows.Shapes;

namespace SeaLauncher
{
    public class MainWindow : Window
    {
        private readonly LibraryState state;
        private readonly List<GameCard> cards = new List<GameCard>();
        private readonly Dictionary<string, GameCard> cardById = new Dictionary<string, GameCard>();
        private readonly Dictionary<NavFilter, NavItem> navItems = new Dictionary<NavFilter, NavItem>();
        private readonly WrapPanel grid = new WrapPanel();
        private readonly SmoothScrollViewer scroller = new SmoothScrollViewer();
        private readonly TextBlock titleText = new TextBlock();
        private readonly TextBlock countText = new TextBlock();
        private readonly TextBox searchBox = new TextBox();
        private readonly TextBlock searchHint = new TextBlock();
        private readonly TextBlock sortLabel = new TextBlock();
        private readonly Border scrollThumb = new Border();
        private readonly StackPanel emptyState = new StackPanel();
        private readonly TextBlock emptyTitle = new TextBlock();
        private readonly TextBlock emptyHint = new TextBlock();
        private readonly Border toast = new Border();
        private readonly TextBlock toastText = new TextBlock();
        private readonly Border dropOverlay = new Border();
        private readonly TextBlock rescanLabel = new TextBlock();
        private readonly RotateTransform rescanSpin = new RotateTransform(0);

        private DateTime lastScan = DateTime.MinValue;
        private NavFilter filter = NavFilter.All;
        private SortMode sort = SortMode.Name;
        private string query = "";
        private bool scanning;

        public MainWindow()
        {
            Title = "Sea Launcher";
            Width = Math.Min(1320, SystemParameters.WorkArea.Width * 0.94);
            Height = Math.Min(820, SystemParameters.WorkArea.Height * 0.94);
            MinWidth = 960;
            MinHeight = 620;
            Background = Theme.Deep;
            FontFamily = Theme.Font;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AllowDrop = true;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            state = Library.Load();
            if (state.Sort >= 0 && state.Sort <= 2)
            {
                sort = (SortMode)state.Sort;
            }

            BuildUi();
            UpdateSortLabel();

            foreach (ImportedEntry entry in state.Imported.ToList())
            {
                if (!string.IsNullOrEmpty(entry.Id) && !string.IsNullOrEmpty(entry.ExePath))
                {
                    AddCard(FromImported(entry));
                }
            }

            SelectFilter(NavFilter.All);

            SourceInitialized += (s, e) => WindowTools.StyleTitleBar(this);
            Loaded += OnLoaded;
            Activated += OnActivated;
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewMouseWheel += (s, e) => MenuPopup.CloseCurrent();
            DragEnter += OnWindowDragEnter;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await RescanAsync(true);
        }

        private void OnActivated(object sender, EventArgs e)
        {
            if (!scanning && (DateTime.UtcNow - lastScan).TotalSeconds > 90)
            {
                var ignored = RescanAsync(true);
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                searchBox.Focus();
                searchBox.SelectAll();
                e.Handled = true;
            }
        }

        private void BuildUi()
        {
            var root = new Grid();
            root.Children.Add(new SeaBackground());

            var layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            FrameworkElement sidebar = BuildSidebar();
            Grid.SetColumn(sidebar, 0);
            layout.Children.Add(sidebar);

            FrameworkElement main = BuildMain();
            Grid.SetColumn(main, 1);
            layout.Children.Add(main);

            root.Children.Add(layout);
            root.Children.Add(BuildDropOverlay());
            root.Children.Add(BuildToast());
            Content = root;
        }

        private FrameworkElement BuildSidebar()
        {
            var shell = new Border();
            shell.Background = Theme.Panel;
            shell.BorderBrush = Theme.Divider;
            shell.BorderThickness = new Thickness(0, 0, 1, 0);

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel();
            header.Orientation = Orientation.Horizontal;
            header.Margin = new Thickness(24, 26, 20, 16);
            header.Children.Add(Logos.WaveMark(38));

            var name = new TextBlock();
            name.FontSize = 20;
            name.VerticalAlignment = VerticalAlignment.Center;
            name.Margin = new Thickness(12, 0, 0, 0);
            name.Inlines.Add(new Run("Sea") { FontWeight = FontWeights.Bold, Foreground = Theme.Text });
            name.Inlines.Add(new Run(" Launcher") { FontWeight = FontWeights.Light, Foreground = Theme.Accent });
            header.Children.Add(name);
            Grid.SetRow(header, 0);
            layout.Children.Add(header);

            var nav = new StackPanel();
            nav.Children.Add(SectionLabel("LIBRARY"));
            AddNav(nav, NavFilter.All, "All Games", Logos.Glyph("\uE7FC", 16, Theme.Accent));
            AddNav(nav, NavFilter.Favorites, "Favorites", Logos.Glyph("\uE735", 16, Theme.Gold));
            AddNav(nav, NavFilter.Recent, "Recently Played", Logos.Glyph("\uE81C", 16, Theme.Accent));
            nav.Children.Add(SectionLabel("PLATFORMS"));
            AddNav(nav, NavFilter.Steam, "Steam", Logos.Badge(Platform.Steam, 22));
            AddNav(nav, NavFilter.Epic, "Epic Games", Logos.Badge(Platform.Epic, 22));
            AddNav(nav, NavFilter.Xbox, "Xbox", Logos.Badge(Platform.Xbox, 22));
            AddNav(nav, NavFilter.Gog, "GOG", Logos.Badge(Platform.Gog, 22));
            AddNav(nav, NavFilter.Imported, "Imported", Logos.Badge(Platform.Imported, 22));
            nav.Children.Add(new Border { Height = 10 });
            AddNav(nav, NavFilter.Hidden, "Hidden", Logos.Glyph("\uED1A", 16, Theme.Muted));

            var navScroll = new SmoothScrollViewer();
            navScroll.Content = nav;
            Grid.SetRow(navScroll, 1);
            layout.Children.Add(navScroll);

            var bottom = new StackPanel();
            bottom.Margin = new Thickness(16, 8, 16, 20);
            bottom.Children.Add(BuildAddButton());
            bottom.Children.Add(BuildRescanButton());
            Grid.SetRow(bottom, 2);
            layout.Children.Add(bottom);

            shell.Child = layout;
            return shell;
        }

        private static TextBlock SectionLabel(string text)
        {
            var label = new TextBlock();
            label.Text = text;
            label.FontSize = 11;
            label.FontWeight = FontWeights.SemiBold;
            label.Foreground = Theme.Muted;
            label.Margin = new Thickness(26, 16, 0, 6);
            return label;
        }

        private void AddNav(Panel parent, NavFilter target, string text, FrameworkElement icon)
        {
            var item = new NavItem(target, text, icon);
            item.Clicked += () => SelectFilter(target);
            navItems[target] = item;
            parent.Children.Add(item);
        }

        private FrameworkElement BuildAddButton()
        {
            var button = new ClickBorder();
            button.Height = 44;
            button.CornerRadius = new CornerRadius(12);
            button.HoverScale = 1.02;
            button.PressScale = 0.97;

            var back = new Border();
            back.CornerRadius = new CornerRadius(12);
            back.Background = Theme.AccentGradient;

            var row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.HorizontalAlignment = HorizontalAlignment.Center;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(Logos.Glyph("\uE710", 14, Theme.Deep));

            var label = new TextBlock();
            label.Text = "Add game";
            label.FontSize = 14;
            label.FontWeight = FontWeights.SemiBold;
            label.Foreground = Theme.Deep;
            label.Margin = new Thickness(9, 0, 0, 0);
            label.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(label);

            back.Child = row;
            button.Child = back;
            button.Clicked += ShowAddDialog;
            return button;
        }

        private FrameworkElement BuildRescanButton()
        {
            var button = new ClickBorder();
            button.Height = 40;
            button.Margin = new Thickness(0, 10, 0, 0);
            button.CornerRadius = new CornerRadius(12);
            button.SetColors(Color.FromArgb(22, 255, 255, 255), Color.FromArgb(48, 255, 255, 255));

            var row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.HorizontalAlignment = HorizontalAlignment.Center;
            row.VerticalAlignment = VerticalAlignment.Center;

            TextBlock glyph = Logos.Glyph("\uE72C", 13, Theme.Text);
            glyph.RenderTransformOrigin = new Point(0.5, 0.5);
            glyph.RenderTransform = rescanSpin;
            row.Children.Add(glyph);

            rescanLabel.Text = "Rescan library";
            rescanLabel.FontSize = 13.5;
            rescanLabel.Foreground = Theme.Text;
            rescanLabel.Margin = new Thickness(9, 0, 0, 0);
            rescanLabel.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(rescanLabel);

            button.Child = row;
            button.Clicked += () =>
            {
                if (!scanning)
                {
                    var ignored = RescanAsync(false);
                }
            };
            return button;
        }

        private FrameworkElement BuildMain()
        {
            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var top = new Grid();
            top.Margin = new Thickness(32, 26, 32, 8);
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var heading = new StackPanel();
            heading.VerticalAlignment = VerticalAlignment.Center;
            titleText.FontSize = 28;
            titleText.FontWeight = FontWeights.SemiBold;
            titleText.Foreground = Theme.Text;
            countText.FontSize = 13;
            countText.Foreground = Theme.Muted;
            countText.Margin = new Thickness(1, 2, 0, 0);
            heading.Children.Add(titleText);
            heading.Children.Add(countText);
            Grid.SetColumn(heading, 0);
            top.Children.Add(heading);

            var tools = new StackPanel();
            tools.Orientation = Orientation.Horizontal;
            tools.VerticalAlignment = VerticalAlignment.Center;
            tools.Children.Add(BuildSearch());
            tools.Children.Add(BuildSortButton());
            Grid.SetColumn(tools, 1);
            top.Children.Add(tools);

            Grid.SetRow(top, 0);
            layout.Children.Add(top);

            var content = new Grid();
            Grid.SetRow(content, 1);

            grid.Margin = new Thickness(23, 8, 23, 40);
            scroller.Content = grid;
            scroller.ScrollChanged += OnScrollChanged;
            content.Children.Add(scroller);

            emptyState.HorizontalAlignment = HorizontalAlignment.Center;
            emptyState.VerticalAlignment = VerticalAlignment.Center;
            emptyState.Margin = new Thickness(0, 0, 0, 60);
            emptyState.IsHitTestVisible = false;
            emptyState.Visibility = Visibility.Collapsed;
            emptyTitle.FontSize = 20;
            emptyTitle.FontWeight = FontWeights.SemiBold;
            emptyTitle.Foreground = Theme.Text;
            emptyTitle.HorizontalAlignment = HorizontalAlignment.Center;
            emptyHint.FontSize = 14;
            emptyHint.Foreground = Theme.Muted;
            emptyHint.Margin = new Thickness(0, 6, 0, 0);
            emptyHint.TextAlignment = TextAlignment.Center;
            emptyHint.HorizontalAlignment = HorizontalAlignment.Center;
            emptyState.Children.Add(emptyTitle);
            emptyState.Children.Add(emptyHint);
            content.Children.Add(emptyState);

            scrollThumb.Width = 4;
            scrollThumb.CornerRadius = new CornerRadius(2);
            scrollThumb.Background = Theme.Solid(Color.FromArgb(96, 120, 230, 240));
            scrollThumb.HorizontalAlignment = HorizontalAlignment.Right;
            scrollThumb.VerticalAlignment = VerticalAlignment.Top;
            scrollThumb.Margin = new Thickness(0, 8, 6, 0);
            scrollThumb.IsHitTestVisible = false;
            scrollThumb.Visibility = Visibility.Collapsed;
            content.Children.Add(scrollThumb);

            layout.Children.Add(content);
            return layout;
        }

        private FrameworkElement BuildSearch()
        {
            var field = new Border();
            field.Width = 300;
            field.Height = 42;
            field.Margin = new Thickness(0, 0, 12, 0);
            field.CornerRadius = new CornerRadius(12);
            field.Background = Theme.FieldBack;
            field.BorderBrush = Theme.Edge;
            field.BorderThickness = new Thickness(1);

            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock icon = Logos.Glyph("\uE721", 14, Theme.Muted);
            Grid.SetColumn(icon, 0);
            inner.Children.Add(icon);

            searchHint.Text = "Search your library";
            searchHint.FontSize = 14;
            searchHint.Foreground = Theme.Muted;
            searchHint.VerticalAlignment = VerticalAlignment.Center;
            searchHint.Margin = new Thickness(2, 0, 0, 0);
            searchHint.IsHitTestVisible = false;
            Grid.SetColumn(searchHint, 1);
            inner.Children.Add(searchHint);

            searchBox.Background = Brushes.Transparent;
            searchBox.BorderThickness = new Thickness(0);
            searchBox.Foreground = Theme.Text;
            searchBox.CaretBrush = Theme.Text;
            searchBox.FontSize = 14;
            searchBox.VerticalContentAlignment = VerticalAlignment.Center;
            searchBox.Padding = new Thickness(0, 0, 12, 0);
            Grid.SetColumn(searchBox, 1);
            inner.Children.Add(searchBox);

            searchBox.TextChanged += OnSearchChanged;
            searchBox.GotKeyboardFocus += (s, e) => field.BorderBrush = Theme.Accent;
            searchBox.LostKeyboardFocus += (s, e) => field.BorderBrush = Theme.Edge;
            searchBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    searchBox.Text = "";
                    Keyboard.ClearFocus();
                }
            };

            field.Child = inner;
            return field;
        }

        private FrameworkElement BuildSortButton()
        {
            var button = new ClickBorder();
            button.Height = 42;
            button.CornerRadius = new CornerRadius(12);
            button.BorderBrush = Theme.Edge;
            button.BorderThickness = new Thickness(1);
            button.SetColors(Color.FromArgb(120, 6, 26, 44), Color.FromArgb(170, 14, 50, 76));

            var row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Margin = new Thickness(16, 0, 18, 0);
            row.Children.Add(Logos.Glyph("\uE8CB", 14, Theme.Accent));

            sortLabel.FontSize = 13.5;
            sortLabel.Foreground = Theme.Text;
            sortLabel.Margin = new Thickness(10, 0, 0, 0);
            sortLabel.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(sortLabel);

            button.Child = row;
            button.Clicked += () =>
            {
                sort = (SortMode)(((int)sort + 1) % 3);
                state.Sort = (int)sort;
                Library.Save(state);
                UpdateSortLabel();
                RefreshGrid();
            };
            return button;
        }

        private FrameworkElement BuildDropOverlay()
        {
            dropOverlay.Background = Theme.ScrimBack;
            dropOverlay.Visibility = Visibility.Collapsed;
            dropOverlay.AllowDrop = true;

            var host = new Grid();

            var dashed = new Shapes.Rectangle();
            dashed.Margin = new Thickness(28);
            dashed.RadiusX = 22;
            dashed.RadiusY = 22;
            dashed.Stroke = Theme.Accent;
            dashed.StrokeThickness = 2;
            dashed.StrokeDashArray = new DoubleCollection { 5, 4 };
            dashed.Fill = Brushes.Transparent;
            dashed.IsHitTestVisible = false;
            host.Children.Add(dashed);

            var label = new TextBlock();
            label.Text = "Drop to add to your library";
            label.FontSize = 24;
            label.FontWeight = FontWeights.SemiBold;
            label.Foreground = Theme.Text;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.IsHitTestVisible = false;
            host.Children.Add(label);

            dropOverlay.Child = host;
            dropOverlay.DragOver += OnOverlayDragOver;
            dropOverlay.DragLeave += (s, e) => dropOverlay.Visibility = Visibility.Collapsed;
            dropOverlay.Drop += OnOverlayDrop;
            return dropOverlay;
        }

        private FrameworkElement BuildToast()
        {
            toast.HorizontalAlignment = HorizontalAlignment.Center;
            toast.VerticalAlignment = VerticalAlignment.Bottom;
            toast.Margin = new Thickness(0, 0, 0, 34);
            toast.Padding = new Thickness(22, 12, 22, 12);
            toast.CornerRadius = new CornerRadius(14);
            toast.Background = Theme.MenuBack;
            toast.BorderBrush = Theme.Edge;
            toast.BorderThickness = new Thickness(1);
            toast.Opacity = 0;
            toast.IsHitTestVisible = false;

            toastText.FontSize = 13.5;
            toastText.Foreground = Theme.Text;
            toast.Child = toastText;
            return toast;
        }

        private void OnWindowDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                dropOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnOverlayDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnOverlayDrop(object sender, DragEventArgs e)
        {
            dropOverlay.Visibility = Visibility.Collapsed;
            e.Handled = true;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                ImportFiles(files);
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double view = scroller.ViewportHeight;
            double extent = scroller.ExtentHeight;
            if (view <= 0 || extent <= view + 1)
            {
                scrollThumb.Visibility = Visibility.Collapsed;
                return;
            }

            double thumb = Math.Max(44, view * view / extent);
            double travel = Math.Max(0, view - thumb - 16);
            double ratio = scroller.VerticalOffset / (extent - view);
            scrollThumb.Height = thumb;
            scrollThumb.Margin = new Thickness(0, 8 + travel * ratio, 6, 0);
            scrollThumb.Visibility = Visibility.Visible;
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            query = searchBox.Text ?? "";
            searchHint.Visibility = query.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            RefreshGrid();
            scroller.JumpToTop();
        }

        private void SelectFilter(NavFilter value)
        {
            filter = value;
            foreach (KeyValuePair<NavFilter, NavItem> pair in navItems)
            {
                pair.Value.SetSelected(pair.Key == value);
            }
            titleText.Text = TitleFor(value);
            RefreshGrid();
            scroller.JumpToTop();
        }

        private static string TitleFor(NavFilter value)
        {
            switch (value)
            {
                case NavFilter.Favorites: return "Favorites";
                case NavFilter.Recent: return "Recently Played";
                case NavFilter.Steam: return "Steam";
                case NavFilter.Epic: return "Epic Games";
                case NavFilter.Xbox: return "Xbox";
                case NavFilter.Gog: return "GOG";
                case NavFilter.Imported: return "Imported";
                case NavFilter.Hidden: return "Hidden Games";
                default: return "All Games";
            }
        }

        private void UpdateSortLabel()
        {
            switch (sort)
            {
                case SortMode.Recent: sortLabel.Text = "Sort: Recent"; break;
                case SortMode.Platform: sortLabel.Text = "Sort: Platform"; break;
                default: sortLabel.Text = "Sort: A-Z"; break;
            }
        }

        private bool Matches(Game game, string text)
        {
            if (filter == NavFilter.Hidden)
            {
                if (!game.Hidden)
                {
                    return false;
                }
            }
            else
            {
                if (game.Hidden)
                {
                    return false;
                }
                switch (filter)
                {
                    case NavFilter.Favorites:
                        if (!game.Favorite) return false;
                        break;
                    case NavFilter.Recent:
                        if (game.LastPlayed == null) return false;
                        break;
                    case NavFilter.Steam:
                        if (game.Platform != Platform.Steam) return false;
                        break;
                    case NavFilter.Epic:
                        if (game.Platform != Platform.Epic) return false;
                        break;
                    case NavFilter.Xbox:
                        if (game.Platform != Platform.Xbox) return false;
                        break;
                    case NavFilter.Gog:
                        if (game.Platform != Platform.Gog) return false;
                        break;
                    case NavFilter.Imported:
                        if (game.Platform != Platform.Imported) return false;
                        break;
                }
            }

            if (text.Length > 0 && game.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) < 0)
            {
                return false;
            }
            return true;
        }

        private List<GameCard> Order(List<GameCard> visible)
        {
            IEnumerable<GameCard> ordered;
            if (filter == NavFilter.Recent || sort == SortMode.Recent)
            {
                ordered = visible
                    .OrderByDescending(c => c.Game.LastPlayed ?? DateTime.MinValue)
                    .ThenBy(c => c.Game.Name, StringComparer.CurrentCultureIgnoreCase);
            }
            else if (sort == SortMode.Platform)
            {
                ordered = visible
                    .OrderBy(c => (int)c.Game.Platform)
                    .ThenBy(c => c.Game.Name, StringComparer.CurrentCultureIgnoreCase);
            }
            else
            {
                ordered = visible.OrderBy(c => c.Game.Name, StringComparer.CurrentCultureIgnoreCase);
            }
            return ordered.ToList();
        }

        private void RefreshGrid()
        {
            string text = query.Trim();
            var visible = new List<GameCard>();
            foreach (GameCard card in cards)
            {
                if (Matches(card.Game, text))
                {
                    visible.Add(card);
                }
            }

            List<GameCard> list = Order(visible);

            bool same = grid.Children.Count == list.Count;
            if (same)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (!ReferenceEquals(grid.Children[i], list[i]))
                    {
                        same = false;
                        break;
                    }
                }
            }
            if (!same)
            {
                grid.Children.Clear();
                foreach (GameCard card in list)
                {
                    grid.Children.Add(card);
                }
            }

            UpdateCounts();
            countText.Text = list.Count == 1 ? "1 game" : list.Count + " games\n\nthe default path for games from Epic Games should be %USERPROFILE%\\Epic Games\nThe fortnite path can be found at C:\\Program Files\\Epic Games.";

            if (list.Count == 0)
            {
                UpdateEmptyText(text);
                emptyState.Visibility = Visibility.Visible;
            }
            else
            {
                emptyState.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateEmptyText(string text)
        {
            if (text.Length > 0)
            {
                emptyTitle.Text = "No matches";
                emptyHint.Text = "Try a different name.";
                return;
            }

            switch (filter)
            {
                case NavFilter.Favorites:
                    emptyTitle.Text = "No favorites yet";
                    emptyHint.Text = "Open the three dots on a game to add it here.";
                    break;
                case NavFilter.Recent:
                    emptyTitle.Text = "Nothing played yet";
                    emptyHint.Text = "Games you launch will show up here.";
                    break;
                case NavFilter.Imported:
                    emptyTitle.Text = "Nothing imported yet";
                    emptyHint.Text = "Click Add game or drop an .exe onto the window.";
                    break;
                case NavFilter.Hidden:
                    emptyTitle.Text = "No hidden games";
                    emptyHint.Text = "Games you hide will show up here.";
                    break;
                case NavFilter.All:
                    emptyTitle.Text = scanning ? "Looking for your games" : "Your library is empty";
                    emptyHint.Text = "Install games on Steam, Epic, Xbox or GOG,\nor drop an .exe here to add it yourself.";
                    break;
                default:
                    emptyTitle.Text = "No " + TitleFor(filter) + " games found";
                    emptyHint.Text = "Install a game there, then click Rescan library.";
                    break;
            }
        }

        private void UpdateCounts()
        {
            int all = 0, favorites = 0, recent = 0, steam = 0, epic = 0, xbox = 0, gog = 0, imported = 0, hidden = 0;
            foreach (GameCard card in cards)
            {
                Game game = card.Game;
                if (game.Hidden)
                {
                    hidden++;
                    continue;
                }
                all++;
                if (game.Favorite) favorites++;
                if (game.LastPlayed != null) recent++;
                switch (game.Platform)
                {
                    case Platform.Steam: steam++; break;
                    case Platform.Epic: epic++; break;
                    case Platform.Xbox: xbox++; break;
                    case Platform.Gog: gog++; break;
                    default: imported++; break;
                }
            }

            SetCount(NavFilter.All, all);
            SetCount(NavFilter.Favorites, favorites);
            SetCount(NavFilter.Recent, recent);
            SetCount(NavFilter.Steam, steam);
            SetCount(NavFilter.Epic, epic);
            SetCount(NavFilter.Xbox, xbox);
            SetCount(NavFilter.Gog, gog);
            SetCount(NavFilter.Imported, imported);
            SetCount(NavFilter.Hidden, hidden);
        }

        private void SetCount(NavFilter target, int value)
        {
            NavItem item;
            if (navItems.TryGetValue(target, out item))
            {
                item.SetCount(value);
            }
        }

        private GameCard AddCard(Game game)
        {
            GameCard existing;
            if (cardById.TryGetValue(game.Id, out existing))
            {
                return existing;
            }

            var card = new GameCard(game, PlayGame, ShowMenu);
            cards.Add(card);
            cardById[game.Id] = card;
            return card;
        }

        private void ApplyState(Game game)
        {
            game.OriginalName = game.Name;

            string name;
            if (state.Names.TryGetValue(game.Id, out name) && !string.IsNullOrWhiteSpace(name))
            {
                game.Name = name;
            }

            string cover;
            if (state.Covers.TryGetValue(game.Id, out cover) && File.Exists(cover))
            {
                game.CoverPath = cover;
            }

            game.Favorite = state.Favorites.Contains(game.Id);
            game.Hidden = state.Hidden.Contains(game.Id);

            DateTime last;
            if (state.LastPlayed.TryGetValue(game.Id, out last))
            {
                game.LastPlayed = last;
            }
            else
            {
                game.LastPlayed = null;
            }
        }

        private Game FromImported(ImportedEntry entry)
        {
            var game = new Game();
            game.Id = entry.Id;
            game.Name = string.IsNullOrWhiteSpace(entry.Name) ? "Game" : entry.Name;
            game.Platform = Platform.Imported;
            game.ExePath = entry.ExePath;
            game.Arguments = entry.Arguments;
            game.InstallDir = SafeDirectory(entry.ExePath);
            ApplyState(game);
            return game;
        }

        private static string SafeDirectory(string path)
        {
            try
            {
                return Path.GetDirectoryName(path);
            }
            catch
            {
                return null;
            }
        }

        private static List<Game> ScanAll()
        {
            var tasks = new Task<List<Game>>[]
            {
                Task.Run(() => Safe(SteamScanner.Scan)),
                Task.Run(() => Safe(EpicScanner.Scan)),
                Task.Run(() => Safe(XboxScanner.Scan)),
                Task.Run(() => Safe(GogScanner.Scan))
            };
            Task.WaitAll(tasks);

            var all = new List<Game>();
            foreach (Task<List<Game>> task in tasks)
            {
                all.AddRange(task.Result);
            }
            return all;
        }

        private static List<Game> Safe(Func<List<Game>> scan)
        {
            try
            {
                return scan();
            }
            catch
            {
                return new List<Game>();
            }
        }

        private async Task RescanAsync(bool silent)
        {
            if (scanning)
            {
                return;
            }

            scanning = true;
            SetScanning(true);
            try
            {
                List<Game> found;
                try
                {
                    found = await Task.Run(() => ScanAll());
                }
                catch (Exception)
                {
                    found = new List<Game>();
                }

                MergeDetected(found);
                if (!silent)
                {
                    ShowToast("Library updated");
                }
            }
            finally
            {
                scanning = false;
                SetScanning(false);
                lastScan = DateTime.UtcNow;
            }
        }

        private void SetScanning(bool value)
        {
            rescanLabel.Text = value ? "Scanning" : "Rescan library";
            if (value)
            {
                var spin = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(900));
                spin.RepeatBehavior = RepeatBehavior.Forever;
                rescanSpin.BeginAnimation(RotateTransform.AngleProperty, spin);
            }
            else
            {
                rescanSpin.BeginAnimation(RotateTransform.AngleProperty, null);
                rescanSpin.Angle = 0;
            }
        }

        private void MergeDetected(List<Game> found)
        {
            var foundIds = new HashSet<string>();
            foreach (Game game in found)
            {
                if (!foundIds.Add(game.Id))
                {
                    continue;
                }

                ApplyState(game);
                GameCard existing;
                if (cardById.TryGetValue(game.Id, out existing))
                {
                    existing.Refresh(game);
                }
                else
                {
                    AddCard(game);
                }
            }

            List<GameCard> gone = cards
                .Where(c => c.Game.Platform != Platform.Imported && !foundIds.Contains(c.Game.Id))
                .ToList();
            foreach (GameCard card in gone)
            {
                cards.Remove(card);
                cardById.Remove(card.Game.Id);
            }

            RefreshGrid();
        }

        private void PlayGame(GameCard card)
        {
            Game game = card.Game;
            string error;
            if (GameLauncher.Launch(game, out error))
            {
                DateTime now = DateTime.Now;
                game.LastPlayed = now;
                state.LastPlayed[game.Id] = now;
                Library.Save(state);
                ShowToast("Launching " + game.Name);
                UpdateCounts();
            }
            else
            {
                ShowToast(error ?? "Could not launch " + game.Name + ".");
            }
        }

        private void ShowMenu(GameCard card, FrameworkElement anchor, bool atMouse)
        {
            Game game = card.Game;
            var items = new List<MenuEntry>();

            items.Add(MenuEntry.Item("\uE768", "Play", () => PlayGame(card), false));
            items.Add(MenuEntry.Item(
                game.Favorite ? "\uE735" : "\uE734",
                game.Favorite ? "Remove from favorites" : "Add to favorites",
                () => ToggleFavorite(card),
                false));
            items.Add(MenuEntry.Separator());
            items.Add(MenuEntry.Item("\uE838", "Open install folder", () => OpenFolder(card), false));
            items.Add(MenuEntry.Item("\uE70F", "Rename", () => RenameGame(card), false));
            items.Add(MenuEntry.Item("\uEB9F", "Change cover image", () => ChangeCover(card), false));
            if (state.Covers.ContainsKey(game.Id))
            {
                items.Add(MenuEntry.Item("\uE7A7", "Reset cover image", () => ResetCover(card), false));
            }
            items.Add(MenuEntry.Separator());

            if (game.Hidden)
            {
                items.Add(MenuEntry.Item("\uE7B3", "Show in library", () => SetHidden(card, false), false));
            }
            else if (game.Platform == Platform.Imported)
            {
                items.Add(MenuEntry.Item("\uE74D", "Remove from Sea Launcher", () => RemoveImported(card), true));
            }
            else
            {
                items.Add(MenuEntry.Item("\uED1A", "Hide from library", () => SetHidden(card, true), false));
            }

            MenuPopup.Show(anchor, atMouse, items);
        }

        private void ToggleFavorite(GameCard card)
        {
            Game game = card.Game;
            game.Favorite = !game.Favorite;
            if (game.Favorite)
            {
                if (!state.Favorites.Contains(game.Id))
                {
                    state.Favorites.Add(game.Id);
                }
            }
            else
            {
                state.Favorites.Remove(game.Id);
            }

            Library.Save(state);
            card.SetFavorite();
            if (filter == NavFilter.Favorites)
            {
                RefreshGrid();
            }
            else
            {
                UpdateCounts();
            }
        }

        private void OpenFolder(GameCard card)
        {
            string folder = FolderFor(card.Game);
            if (folder == null)
            {
                ShowToast("Could not find the game folder.");
                return;
            }

            try
            {
                var info = new ProcessStartInfo("explorer.exe", "\"" + folder + "\"");
                info.UseShellExecute = true;
                Process process = Process.Start(info);
                if (process != null)
                {
                    process.Dispose();
                }
            }
            catch (Exception)
            {
                ShowToast("Could not open the folder.");
            }
        }

        private static string FolderFor(Game game)
        {
            if (!string.IsNullOrEmpty(game.InstallDir) && Directory.Exists(game.InstallDir))
            {
                return game.InstallDir;
            }
            if (!string.IsNullOrEmpty(game.ExePath))
            {
                string folder = SafeDirectory(game.ExePath);
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    return folder;
                }
            }
            return null;
        }

        private void RenameGame(GameCard card)
        {
            Game game = card.Game;
            var prompt = new PromptWindow("Rename game", "Game name (leave empty to restore the original)", game.Name);
            prompt.Owner = this;
            if (prompt.ShowDialog() != true)
            {
                return;
            }

            string value = prompt.Result;
            if (string.IsNullOrWhiteSpace(value) || value == game.OriginalName)
            {
                state.Names.Remove(game.Id);
                game.Name = game.OriginalName ?? game.Name;
            }
            else
            {
                state.Names[game.Id] = value;
                game.Name = value;
            }

            Library.Save(state);
            card.Refresh(game);
            RefreshGrid();
        }

        private void ChangeCover(GameCard card)
        {
            Game game = card.Game;
            var dialog = new OpenFileDialog();
            dialog.Title = "Choose a cover image";
            dialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Library.CoverDir);
                string extension = Path.GetExtension(dialog.FileName);
                string target = Path.Combine(Library.CoverDir, FileUtil.SafeName(game.Id) + "_" + DateTime.Now.Ticks + extension);
                File.Copy(dialog.FileName, target, true);

                if (ImageTools.LoadFile(target, 340) == null)
                {
                    DeleteCoverFile(target);
                    ShowToast("That image format is not supported.");
                    return;
                }

                string previous;
                if (state.Covers.TryGetValue(game.Id, out previous))
                {
                    DeleteCoverFile(previous);
                }

                state.Covers[game.Id] = target;
                game.CoverPath = target;
                Library.Save(state);
                card.ReloadArt();
                ShowToast("Cover updated");
            }
            catch (Exception)
            {
                ShowToast("Could not use that image.");
            }
        }

        private void ResetCover(GameCard card)
        {
            Game game = card.Game;
            string previous;
            if (state.Covers.TryGetValue(game.Id, out previous))
            {
                DeleteCoverFile(previous);
                state.Covers.Remove(game.Id);
            }

            game.CoverPath = null;
            Library.Save(state);
            card.ReloadArt();
            if (game.Platform != Platform.Imported)
            {
                var ignored = RescanAsync(true);
            }
        }

        private static void DeleteCoverFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                string root = Path.GetFullPath(Library.CoverDir);
                string full = Path.GetFullPath(path);
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
                {
                    File.Delete(full);
                }
            }
            catch
            {
            }
        }

        private void SetHidden(GameCard card, bool hidden)
        {
            Game game = card.Game;
            game.Hidden = hidden;
            if (hidden)
            {
                if (!state.Hidden.Contains(game.Id))
                {
                    state.Hidden.Add(game.Id);
                }
            }
            else
            {
                state.Hidden.Remove(game.Id);
            }

            Library.Save(state);
            RefreshGrid();
            ShowToast(hidden ? game.Name + " is hidden. Find it under Hidden." : game.Name + " is back in your library.");
        }

        private void RemoveImported(GameCard card)
        {
            Game game = card.Game;
            string id = game.Id;

            state.Imported.RemoveAll(entry => entry.Id == id);
            state.Favorites.Remove(id);
            state.Hidden.Remove(id);
            state.Names.Remove(id);
            state.LastPlayed.Remove(id);

            string cover;
            if (state.Covers.TryGetValue(id, out cover))
            {
                DeleteCoverFile(cover);
                state.Covers.Remove(id);
            }

            cards.Remove(card);
            cardById.Remove(id);
            Library.Save(state);
            RefreshGrid();
            ShowToast(game.Name + " was removed from Sea Launcher.");
        }

        private void ShowAddDialog()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Add games to Sea Launcher";
            dialog.Filter = "Games and shortcuts|*.exe;*.lnk;*.bat;*.cmd;*.url|All files|*.*";
            dialog.Multiselect = true;
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog(this) == true)
            {
                ImportFiles(dialog.FileNames);
            }
        }

        private void ImportFiles(IEnumerable<string> paths)
        {
            int added = 0;
            string lastName = null;
            string problem = null;

            foreach (string path in paths)
            {
                string issue;
                Game game = TryImport(path, out issue);
                if (game != null)
                {
                    added++;
                    lastName = game.Name;
                }
                else if (issue != null)
                {
                    problem = issue;
                }
            }

            if (added == 0)
            {
                if (problem != null)
                {
                    ShowToast(problem);
                }
                return;
            }

            if (!string.IsNullOrEmpty(query))
            {
                searchBox.Text = "";
            }
            if (filter != NavFilter.All && filter != NavFilter.Imported)
            {
                SelectFilter(NavFilter.Imported);
            }
            else
            {
                RefreshGrid();
            }

            ShowToast(added == 1 ? "Added " + lastName : "Added " + added + " games");
        }

        private Game TryImport(string path, out string issue)
        {
            issue = null;
            try
            {
                if (!File.Exists(path))
                {
                    issue = "Drop an .exe or a shortcut to add a game.";
                    return null;
                }

                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension != ".exe" && extension != ".lnk" && extension != ".bat" && extension != ".cmd" && extension != ".url")
                {
                    issue = "Only .exe files and shortcuts can be added.";
                    return null;
                }

                string full = Path.GetFullPath(path);
                string id = "imp:" + FileUtil.ShortHash(full.ToLowerInvariant());
                if (cardById.ContainsKey(id))
                {
                    issue = "That game is already in your library.";
                    return null;
                }

                var entry = new ImportedEntry();
                entry.Id = id;
                entry.Name = GuessName(full);
                entry.ExePath = full;
                state.Imported.Add(entry);
                Library.Save(state);

                Game game = FromImported(entry);
                AddCard(game);
                return game;
            }
            catch (Exception)
            {
                issue = "Could not add that file.";
                return null;
            }
        }

        private static string GuessName(string path)
        {
            string name = null;
            try
            {
                if (string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
                    string product = info.ProductName;
                    if (!string.IsNullOrWhiteSpace(product))
                    {
                        product = product.Trim();
                        bool generic = product.Equals("UnrealEngine", StringComparison.OrdinalIgnoreCase)
                            || product.Equals("Unreal Engine", StringComparison.OrdinalIgnoreCase)
                            || product.Equals("Unity", StringComparison.OrdinalIgnoreCase)
                            || product.Equals("Unity Player", StringComparison.OrdinalIgnoreCase);
                        if (!generic && product.Length <= 60)
                        {
                            name = product;
                        }
                    }
                }
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = Path.GetFileNameWithoutExtension(path);
            }
            return name;
        }

        private void ShowToast(string message)
        {
            toastText.Text = message;

            var animation = new DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2600))));
            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2900))));
            toast.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
}
