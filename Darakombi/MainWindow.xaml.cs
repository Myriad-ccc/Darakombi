global using Microsoft.Win32;
global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Text;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Documents;
global using System.Windows.Input;
global using System.Windows.Media;
global using static Darakombi.QOL;
using System.Collections;

namespace Darakombi
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch Uptime = Stopwatch.StartNew();
        [DebugWatch(f: "mm\\:ss")]
        private TimeSpan RunTime => Uptime.Elapsed;
        private double lastFrame;
        private bool Active = true;

        private bool WindowDragging;
        private Point WindowDragOffset;

        private readonly HashSet<Key> PressedKeys = [];

        private Rect Viewport
        {
            get
            {
                var scaleX = CameraScale.ScaleX == 0 ? 1 : CameraScale.ScaleX;
                var scaleY = CameraScale.ScaleY == 0 ? 1 : CameraScale.ScaleY;
                var width = GameCanvas.ActualWidth == 0 ? GameCanvas.Width : GameCanvas.ActualWidth;
                var height = GameCanvas.ActualHeight == 0 ? GameCanvas.Height : GameCanvas.ActualHeight;
                return new(
                    -CameraTransform.X / scaleX,
                    -CameraTransform.Y / scaleY,
                    width / scaleX,
                    height / scaleY);
            }
        }
        private Point CameraCenter => new(
            ActualWidth / (CameraScale.ScaleX * 2) - CameraTransform.X,
            ActualHeight / (CameraScale.ScaleY * 2) - CameraTransform.Y);

        private GameManager GameManager;
        private EditorManager EditorManager;
        private IManager CurrentMode;

        private Map Map;
        private Size MapSize = new(12800, 12800);
        private readonly Size SmallestMap = new(100, 100);
        private readonly Size LargestMap = new(20000, 20000);

        private GlobalContext Context;
        private SolidColorBrush EditorTileColor
        {
            get
            {
                var color = new SolidColorBrush(Color.FromRgb((byte)RedSlider?.Value, (byte)GreenSlider?.Value, (byte)BlueSlider?.Value));
                RedSliderText.Foreground = color;
                GreenSliderText.Foreground = color;
                BlueSliderText.Foreground = color;
                return color;
            }
        }

        private Player Player => (CurrentMode as GameManager)?.Player;
        private SpatialGrid SpatialGrid => (CurrentMode as GameManager)?.SpatialGrid;
        private Editor Editor => (CurrentMode as EditorManager)?.Editor;

        private Dictionary<object, SolidColorBrush> ResourceColors = [];
        private Brush GetResourceColor(string color)
        {
            ResourceColors.TryGetValue(color, out var brush);
            return brush;
        }

        public MainWindow()
        {
            InitializeComponent();

            lastFrame = Uptime.Elapsed.TotalSeconds;

            TitleText.Foreground = RandomColor();
            TitleTextShadow.Foreground = RandomColor();

            foreach (DictionaryEntry resource in Resources)
                if (resource.Key is string color && resource.Value is SolidColorBrush brush)
                    ResourceColors[color] = brush;

            ConsoleManager.RegisterStaticCommands();
            ConsoleManager.RegisterInstanceCommands(this);
            ConsoleLogger.Logs = ConsoleLogs;

            MapWidthContent.Text = MapSize.Width.ToString();
            MapHeightContent.Text = MapSize.Height.ToString();
            DebugManager.OnRegistryChanged += BuildDebugMenu;
            DebugManager.Track(this, "Main");
        }

        private void CreateConsoleButton(string name, StackPanel group, Action onClick, Func<bool> getState)
        {
            var panel = new StackPanel() { Orientation = Orientation.Horizontal };

            var fontSize = 36f;
            var background = Brushes.Transparent;

            var onTag = GetResourceColor("Green");
            var offTag = GetResourceColor("Red");

            Style style = (Style)FindResource("MenuButtonStyle");

            var toggle = new Button()
            {
                FontSize = fontSize,
                Background = background,
                BorderThickness = new(0),
                Style = style,
            };

            var text = new TextBlock()
            {
                Text = name,
                FontSize = fontSize,
                Foreground = Brushes.White,
                Background = background,
            };

            void Update()
            {
                var on = getState();
                if (on)
                {
                    toggle.Content = "ON";
                    toggle.Tag = onTag;
                }
                else
                {
                    toggle.Content = "OFF";
                    toggle.Tag = offTag;
                }
            }
            toggle.Click += (s, ev) => { onClick(); Update(); };

            panel.Children.Add(toggle);
            panel.Children.Add(text);
            Update();

            group.Children.Add(panel);
        }
        private void BuildDebugMenu()
        {
            DebugCategoryTabs.Children.Clear();

            foreach (var category in DebugManager.Registry.OrderByDescending(c => c.Value.Count))
            {
                var tabPanel = new StackPanel() { Orientation = Orientation.Vertical };
                DebugCategoryTabs.Children.Add(tabPanel);

                var tabTitle = (new TextBlock
                {
                    Text = category.Key,
                    FontSize = 32,
                    Foreground = Brushes.Gray
                });
                tabPanel.Children.Add(tabTitle);

                var tabItems = new StackPanel() { Orientation = Orientation.Vertical };
                tabPanel.Children.Add(tabItems);

                tabTitle.MouseDown += (s, ev) =>
                {
                    if (ev.ChangedButton == MouseButton.Left)
                        tabItems.Visibility = tabItems.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                };

                foreach (var item in category.Value.OrderBy(x => x.Name))
                {
                    var indicatorAndAttribute = new StackPanel() { Orientation = Orientation.Horizontal };

                    var attributeName = new TextBlock()
                    {
                        Text = $"{item.Name}",
                        Foreground = Brushes.White,
                        FontSize = 32,
                        MaxWidth = 220,
                        TextTrimming = TextTrimming.WordEllipsis,
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var indicator = new Button()
                    {
                        Content = "・",
                        Tag = item.Active ? Brushes.LightGreen : Brushes.IndianRed,
                        Style = (Style)FindResource("MenuButtonStyle"),
                        Background = Brushes.Transparent,
                        BorderThickness = new(0),
                        FontSize = attributeName.FontSize,
                        Width = attributeName.Height,
                        Height = attributeName.Height,
                        MinWidth = 30,
                        MinHeight = 30,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                    };
                    indicator.Click += (s, ev) =>
                    {
                        item.Active = !item.Active;
                        indicator.Tag = item.Active ? Brushes.LightGreen : Brushes.IndianRed;
                        tabTitle.Foreground = category.Value.Any(x => x.Active) ? Brushes.Gold : Brushes.Gray;
                    };

                    indicatorAndAttribute.Children.Add(indicator);
                    indicatorAndAttribute.Children.Add(attributeName);

                    tabItems.Children.Add(indicatorAndAttribute);
                }
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                WindowDragging = true;
                var currentMousePos = PointToScreen(e.GetPosition(this));
                WindowDragOffset = new(currentMousePos.X - Left, currentMousePos.Y - Top);
                Mouse.Capture((UIElement)sender);
            }
            if (e.ChangedButton == MouseButton.Right)
            {
                if (IsPointOverElement(e, TitleText) || IsPointOverElement(e, TitleTextShadow))
                {
                    TitleText.Foreground = RandomColor();
                    TitleTextShadow.Foreground = RandomColor();
                }
            }
        }
        private void TitleBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                WindowDragging = false;
                Mouse.Capture(null);
            }
        }
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && WindowDragging)
            {
                var currentMousePos = PointToScreen(e.GetPosition(this));
                Left = currentMousePos.X - WindowDragOffset.X;
                Top = currentMousePos.Y - WindowDragOffset.Y;
            }
        }
        private void EscapeMenuButton_Click(object sender, RoutedEventArgs e) => Escape();
        private void ClosingButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void ConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            PressedKeys.Clear();
            DebugMenu.Visibility = Visibility.Collapsed;
            DebugText.Visibility = Visibility.Collapsed;

            if (IsVis(ConsoleMenu))
            {
                ConsoleMenu.Visibility = Visibility.Collapsed;
                DebugText.Visibility = Visibility.Visible;
            }
            else
            {
                ConsoleMenu.Visibility = Visibility.Visible;
                CommandLine.Focus();
            }
        }
        private void DebugMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsVis(ConsoleMenu)) return;
            if (IsVis(DebugMenu))
            {
                DebugText.Visibility = Visibility.Visible;
                DebugMenu.Visibility = Visibility.Collapsed;
            }
            else
            {
                DebugText.Visibility = Visibility.Collapsed;
                DebugMenu.Visibility = Visibility.Visible;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            EscapeMenu.Visibility = Visibility.Collapsed;
            Exit();
        }
        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private HashSet<Key> IgnoredKeys = [Key.LeftAlt, Key.RightAlt, Key.Capital, Key.LWin, Key.RWin];
        private Key GetRealKey(KeyEventArgs e) => (e.Key == Key.System) ? e.SystemKey : e.Key;
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = GetRealKey(e);
            if (IgnoredKeys.Contains(key))
                e.Handled = true;
            if (key == Key.Tab && MapMenu.Visibility == Visibility.Collapsed)
                e.Handled = true;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var k = GetRealKey(e);
            if (IgnoredKeys.Contains(k)) return;
            if (!e.IsRepeat && !(IsVis(ConsoleMenu) && k != Key.Escape))
                PressedKeys.Add(k);

            if (PressedKeys.Contains(Key.Escape)) Escape();
            if (PressedKeys.Contains(Key.OemTilde)) DebugMenuButton_Click(null, null);
            if (PressedKeys.Contains(Key.OemQuestion))
            {
                ConsoleButton_Click(null, null);
                e.Handled = true;
            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            var key = GetRealKey(e);
            if (IgnoredKeys.Contains(key)) return;
            PressedKeys.Remove(key);
        }

        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CurrentMode is EditorManager em)
                em.OnMouseDown(sender, e, e.GetPosition(GameCanvas), EditorTileColor);
        }
        private void GameCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentMode is EditorManager em)
                em.OnMouseUp(sender, e, e.GetPosition(GameCanvas));
        }
        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (CurrentMode is EditorManager em)
                em.OnMouseMove(e, e.GetPosition(GameCanvas), EditorTileColor);
        }
        private void GameCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CurrentMode is EditorManager em)
                em.OnMouseWheel(e, e.GetPosition(GameCanvas));
        }

        private void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            EditorQuitWarning.Visibility = Visibility.Collapsed;
            if (EditorManager.Editor?.Tiles.Count > 0)
            {
                EditorManager?.Editor?.Save("MapSave.txt", Map.Size);
                EditorManager?.Editor?.Saved = true;
            }
        }

        private void MapWidthContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MapWidthContent.Text, out int width))
                if (width >= 100 && width <= 20000)
                {
                    SetMapSizeButton.IsHitTestVisible = true;
                    SetMapSizeButton.Foreground = Brushes.LightGreen;
                    return;
                }
            SetMapSizeButton.IsHitTestVisible = false;
            SetMapSizeButton.Foreground = Brushes.IndianRed;
        }
        private void MapHeightContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MapHeightContent.Text, out int height))
                if (height >= 100 && height <= 20000)
                {
                    SetMapSizeButton.IsHitTestVisible = true;
                    SetMapSizeButton.Foreground = Brushes.LightGreen;
                    return;
                }
            SetMapSizeButton.IsHitTestVisible = false;
            SetMapSizeButton.Foreground = Brushes.IndianRed;
        }
        private void ResetMapDimensions_Click(object sender, RoutedEventArgs e)
        {
            if (MapWidthContent is null || MapHeightContent is null) return;
            MapWidthContent.Text = "12800";
            MapHeightContent.Text = "12800";
        }
        private void SetMapSizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MapWidthContent.Text, out int width) && int.TryParse(MapHeightContent.Text, out int height))
            {
                if (TryMapSize(width, height))
                {
                    MapMenu.Visibility = Visibility.Collapsed;
                    StartMenu.Visibility = Visibility.Visible;
                }
            }
        }
        private bool TryMapSize(int width, int height)
        {
            var size = new Size(width, height);
            if (SizeInRange(size, SmallestMap.Width, LargestMap.Width, SmallestMap.Height, LargestMap.Height))
            {
                MapSize = size;
                ResizeMap();
                EditorManager?.EditorGrid?.UpdateGridSize(size);
                EditorManager?.ChunkGrid?.UpdateGridSize(size);
                return true;
            }
            return false;
        }
        private void TryMapSize(Size size)
        {
            if (SizeInRange(size, SmallestMap.Width, LargestMap.Width, SmallestMap.Height, LargestMap.Height))
            {
                MapSize = size;
                ResizeMap();
                EditorManager?.EditorGrid?.UpdateGridSize(size);
                EditorManager?.ChunkGrid?.UpdateGridSize(size);
            }
        }

        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            StartMenu.Visibility = Visibility.Collapsed;
            MapMenu.Visibility = Visibility.Visible;
            MapButton.Foreground = Brushes.White;
        }
        private void EditorDiscardButton_Click(object sender, RoutedEventArgs e)
        {
            EditorManager.Editor?.Clear();
            EditorQuitWarning.Visibility = Visibility.Collapsed;
        }

        private void AddElementToCanvas(UIElement element, int zIndex)
        {
            WorldCanvas.Children.Add(element);
            Canvas.SetLeft(element, 0);
            Canvas.SetTop(element, 0);
            Panel.SetZIndex(element, zIndex);
        }
        private void RemoveElementFromCanvas(UIElement element) => WorldCanvas.Children.Remove(element);
        private void CreateMap()
        {
            Map = new(MapSize);
            AddElementToCanvas(Map, 0);
            MapButton.Foreground = Brushes.White;
            MapSizeText.Text = $"{MapSize.Width}x{MapSize.Height}";
        }
        private void ResizeMap()
        {
            if (Map != null)
            {
                Map.Width = MapSize.Width;
                Map.Height = MapSize.Height;
            }
            else CreateMap();
            MapSizeText.Text = $"{MapSize.Width}x{MapSize.Height}";
        }

        private void ClearArea(Rect area)
        {
            var targets = SpatialGrid.Search(area);
            var toRemove = targets.Where(e => e.Entity is not Darakombi.Player);

            foreach (var data in toRemove)
            {
                GameManager?.Renderer?.Remove(data);
                GameManager?.Game?.AllEntityData.Remove(data);
                if (data.Entity is Rock rock) GameManager?.Game?.Rocks.Remove(rock);
                GameManager?.Game?.SpatialGrid.Remove(data);
            }
        }
        private void ClearViewport()
        {
            ClearArea(Viewport);
        }
        private void ClearMap()
        {
            foreach (var data in GameManager?.Game?.AllEntityData)
                if (data.Entity is not Darakombi.Player)
                    GameManager?.Renderer?.Remove(data);

            GameManager?.Game?.Rocks.Clear();
            GameManager?.Game?.Entities.RemoveAll(e => e is not Darakombi.Player);
            GameManager?.Game?.AllEntityData.RemoveAll(e => e.Entity is not Darakombi.Player);
            GameManager?.Game?.SpatialGrid.ClearAll();
            GameManager?.Game?.SpatialGrid.Add(GameManager?.Game?.AllEntityData[0]);
            GameManager?.Renderer?.ClearCache();
            GC.Collect();
        }
        private void ResetTransforms()
        {
            CameraScale.ScaleX = 1;
            CameraScale.ScaleY = 1;
            CameraSkew.AngleX = 0;
            CameraSkew.AngleY = 0;
            CameraRotate.Angle = 0;
            CameraTransform.X = 0;
            CameraTransform.Y = 0;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager == null)
            {
                GameManager = new(new(WorldCanvas));
                GameManager.AddElementToCanvas += AddElementToCanvas;
                GameManager.RemoveElementFromCanvas += RemoveElementFromCanvas;
                GameManager.ClearViewport += ClearViewport;
                GameManager.ClearMap += ClearMap;
            }
            InitializeMode(GameManager);
        }
        private void DisposeGameManager()
        {
            GameManager.AddElementToCanvas -= AddElementToCanvas;
            GameManager.RemoveElementFromCanvas -= RemoveElementFromCanvas;
            GameManager.ClearViewport -= ClearViewport;
            GameManager.ClearMap -= ClearMap;
            GameManager = null;
        }
        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditorManager == null)
            {
                EditorManager = new();
                EditorManager.HUD = EditorHUD;
                EditorManager.AddElementToCanvas += AddElementToCanvas;
                EditorManager.ResizeMap += TryMapSize;
            }
            InitializeMode(EditorManager);
        }
        private void DisposeEditorManager()
        {
            EditorManager.AddElementToCanvas -= AddElementToCanvas;
            EditorManager.RemoveElementFromCanvas -= RemoveElementFromCanvas;
            EditorManager.ResizeMap -= TryMapSize;
            EditorManager = null;
        }

        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Pick serialized map",
                Filter = "Text files (*.txt)|*.txt",
                DefaultExt = ".txt",
                Multiselect = false,
            };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                EditorButton_Click(null, null);
                EditorManager.Editor?.Clear();
                EditorManager.Editor?.Load(dialog.FileName);
            }
        }

        private void InitializeMode(IManager mode)
        {
            if (CurrentMode == mode) return;
            CurrentMode = mode;
            StartMenu.Visibility = Visibility.Collapsed;
            Start();
        }

        private void Start()
        {
            if (CurrentMode == null) return;

            GameCanvas.Visibility = Visibility.Visible;
            if (Map == null) CreateMap();

            Context = new()
            {
                Viewport = Viewport,
                Map = Map,
                PressedKeys = PressedKeys,
                ScaleTransform = CameraScale,
                SkewTransform = CameraSkew,
                RotateTransform = CameraRotate,
                TranslateTransform = CameraTransform
            };
            CurrentMode.Context = Context;
            Active = CurrentMode.Active = true;
            CurrentMode.Start();

            CompositionTarget.Rendering += OnRender;
        }
        private void Update(double dt)
        {
            if (Active)
            {
                TopKeys.Text = string.Join(',', PressedKeys.Take(4));
                if (CurrentMode != null)
                {
                    CurrentMode.Context.Viewport = Viewport;
                    CurrentMode.Update(dt);
                    DebugText.Text = DebugManager.GetDebugString();
                    RuntimeDebugText.Text = $"fps:{GetAverageFPS(dt):F0}";
                }
            }
        }
        private void Pause() => CurrentMode.Active = Active = false;
        private void Resume() => CurrentMode.Active = Active = true;
        private void Exit()
        {
            DebugManager.Clear();
            if (CurrentMode == null) return;
            CompositionTarget.Rendering -= OnRender;
            CurrentMode.End();

            if (CurrentMode is GameManager) DisposeGameManager();
            if (CurrentMode is EditorManager)
            {
                DisposeEditorManager();
            }

            Map = null;
            WorldCanvas.Children.Clear();
            ResetTransforms();

            GameCanvas.Visibility = Visibility.Collapsed;
            StartMenu.Visibility = Visibility.Visible;
        }

        private double CurrentFrame()
        {
            double now = Uptime.Elapsed.TotalSeconds;
            double dt = Math.Min(now - lastFrame, 0.05);
            lastFrame = now;
            return dt;
        }
        private void OnRender(object sender, EventArgs e) => Update(CurrentFrame());

        private void Escape()
        {
            if (IsVis(ConsoleMenu))
            {
                ConsoleMenu.Visibility = Visibility.Collapsed;
                return;
            }

            DebugMenu.Visibility = Visibility.Collapsed;

            if (CurrentMode == null) return;
            if (Active)
            {
                EscapeMenu.Visibility = Visibility.Visible;
                Pause();
            }
            else
            {
                EscapeMenu.Visibility = Visibility.Collapsed;
                Resume();
            }
        }

        private void EntityPicker_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CommandLine_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string input = CommandLine.Text;
                if (input.Length > 0)
                {
                    MapCommand(input);
                    CommandLine.Clear();
                }
            }
            else if (e.Key == Key.F1)
            {
                ConsoleLogger.IncludeTimeStamps = !ConsoleLogger.IncludeTimeStamps;
            }
        }
        private void MapCommand(string input)
        {
            ConsoleLogger.Log(ConsoleManager.Execute(input));
        }

        private void CommandLine_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        [Command("Greet")]
        private void Greet(string name) => WriteOut($"hi {name}");
    }
}