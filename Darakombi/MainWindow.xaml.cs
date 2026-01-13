global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Text;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Input;
global using System.Windows.Media;
using Microsoft.Win32;

namespace Darakombi
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch Uptime = Stopwatch.StartNew();
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

        public MainWindow()
        {
            InitializeComponent();

            lastFrame = Uptime.Elapsed.TotalSeconds;

            TitleText.Foreground = QOL.RandomColor();
            TitleTextShadow.Foreground = QOL.RandomColor();

            MapSizeText.Text = $"{MapSize.Width}x{MapSize.Height}";
            MapWidthContent.Text = MapSize.Width.ToString();
            MapHeightContent.Text = MapSize.Height.ToString();
            DebugManager.OnRegistryChanged += BuildDebugMenu;
        }

        private void BuildDebugMenu()
        {
            CategoryTabs.Children.Clear();

            foreach (var category in DebugManager.Registry)
            {
                var tabPanel = new StackPanel() { Orientation = Orientation.Vertical };
                CategoryTabs.Children.Add(tabPanel);

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

                foreach (var item in category.Value)
                {
                    var ButtonTextPair = new StackPanel() { Orientation = Orientation.Horizontal, };
                    var attributeName = new TextBlock()
                    {
                        Text = $"{item.Name}",
                        Foreground = Brushes.White,
                        FontSize = 28,
                        Background = Brushes.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    var toggle = new Button()
                    {
                        Content = item.Active ? "✓" : "✗",
                        Tag = item.Active ? Brushes.LightGreen : Brushes.IndianRed,
                        Style = (Style)FindResource("MenuButtonStyle"),
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Beige,
                        BorderThickness = new(3),
                        FontSize = attributeName.FontSize,
                        Width = attributeName.Height,
                        Height = attributeName.Height,
                        MinWidth = 40,
                        MinHeight = 40,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                    };
                    toggle.Click += (s, ev) =>
                    {
                        item.Active = !item.Active;
                        toggle.Content = item.Active ? "✓" : "✗";
                        toggle.Tag = item.Active ? Brushes.LightGreen : Brushes.IndianRed;
                        tabTitle.Foreground = category.Value.Any(x => x.Active) ? Brushes.Gold : Brushes.Gray;
                    };
                    ButtonTextPair.Children.Add(toggle);
                    ButtonTextPair.Children.Add(attributeName);
                    tabItems.Children.Add(ButtonTextPair);
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
                if (QOL.IsPointOverElement(e, TitleText) || QOL.IsPointOverElement(e, TitleTextShadow))
                {
                    TitleText.Foreground = QOL.RandomColor();
                    TitleTextShadow.Foreground = QOL.RandomColor();
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
        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ExitMenuButton_Click(object sender, RoutedEventArgs e)
        {
            EscapeMenu.Visibility = Visibility.Hidden;
            Exit();
        }
        private void DebugMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentMode == null) return;
            if (QOL.IsVis(DebugMenu))
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

        private HashSet<Key> IgnoredKeys = [Key.LeftAlt, Key.RightAlt, Key.Capital, Key.LWin, Key.RWin];
        private Key GetRealKey(KeyEventArgs e) => (e.Key == Key.System) ? e.SystemKey : e.Key;
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = GetRealKey(e);
            if (IgnoredKeys.Contains(key))
                e.Handled = true;
            if (key == Key.Tab && MapMenu.Visibility == Visibility.Hidden)
                e.Handled = true;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var key = GetRealKey(e);
            if (IgnoredKeys.Contains(key)) return;
            if (!e.IsRepeat) PressedKeys.Add(key);

            if (PressedKeys.Contains(Key.Escape)) Escape();
            if (PressedKeys.Contains(Key.OemTilde)) DebugMenuButton_Click(null, null);
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

        private void GhostTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isGhost = Player.CollisionType == CollisionType.Ghost;
            if (isGhost)
            {
                Player.CollisionType = CollisionType.Live;
                GhostBool.Content = "OFF";
                GhostBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                Player.CollisionType = CollisionType.Ghost;
                GhostBool.Content = "ON";
                GhostBool.Foreground = Brushes.LightGreen;
            }
        }
        private void OverlayTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isShown = GameManager.Renderer.ShowOverlays;
            if (isShown)
            {
                GameManager?.Renderer?.ShowOverlays = false;
                OverlayBool.Content = "OFF";
                OverlayBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                GameManager?.Renderer?.ShowOverlays = true;
                OverlayBool.Content = "ON";
                OverlayBool.Foreground = Brushes.LightGreen;
            }
        }
        private void GridTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isShown = GameManager?.SpatialCellGrid?.Visibility == Visibility.Visible;
            if (isShown)
            {
                GameManager?.SpatialCellGrid?.Visibility = Visibility.Hidden;
                GridBool.Content = "OFF";
                GridBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                GameManager?.SpatialCellGrid?.Visibility = Visibility.Visible;
                GridBool.Content = "ON";
                GridBool.Foreground = Brushes.LightGreen;
            }
        }

        private void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            EditorQuitWarning.Visibility = Visibility.Hidden;
            if (EditorManager.Editor?.Tiles.Count > 0)
            {
                EditorManager?.Editor?.Save("MapSave.txt", Map.Size);
                EditorManager?.Editor?.Saved = true;
            }
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
                    MapMenu.Visibility = Visibility.Hidden;
                    StartMenu.Visibility = Visibility.Visible;
                }
            }
        }
        private bool TryMapSize(int width, int height)
        {
            var size = new Size(width, height);
            if (QOL.SizeInRange(size, SmallestMap.Width, LargestMap.Width, SmallestMap.Height, LargestMap.Height))
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
            if (QOL.SizeInRange(size, SmallestMap.Width, LargestMap.Width, SmallestMap.Height, LargestMap.Height))
            {
                MapSize = size;
                ResizeMap();
                EditorManager?.EditorGrid?.UpdateGridSize(size);
                EditorManager?.ChunkGrid?.UpdateGridSize(size);
            }
        }

        private void DrawOverTitle_Click(object sender, RoutedEventArgs e)
        {
            if (EditorManager.Editor.DrawOver)
            {
                DrawOverBool.Content = "OFF";
                DrawOverBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                DrawOverBool.Content = "ON";
                DrawOverBool.Foreground = Brushes.LightGreen;
            }
            EditorManager.Editor?.DrawOver = !EditorManager.Editor.DrawOver;
        }
        private void EditorGridTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isShown = EditorManager.EditorGrid?.Visibility == Visibility.Visible;
            if (isShown)
            {
                EditorManager.EditorGrid?.Visibility = Visibility.Hidden;
                EditorGridBool.Content = "OFF";
                EditorGridBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                EditorManager.EditorGrid?.Visibility = Visibility.Visible;
                EditorGridBool.Content = "ON";
                EditorGridBool.Foreground = Brushes.LightGreen;
            }
        }
        private void EditorChunkGridTitle_Click(object sender, RoutedEventArgs e)
        {
            if (EditorManager?.ChunkGrid?.Visibility == Visibility.Visible)
            {
                EditorManager?.ChunkGrid?.Visibility = Visibility.Hidden;
                EditorChunkGridBool.Foreground = Brushes.IndianRed;
                EditorChunkGridBool.Content = "OFF";
            }
            else
            {
                EditorManager?.ChunkGrid?.Visibility = Visibility.Visible;
                EditorChunkGridBool.Foreground = Brushes.LightGreen;
                EditorChunkGridBool.Content = "ON";
            }
        }

        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            StartMenu.Visibility = Visibility.Hidden;
            MapMenu.Visibility = Visibility.Visible;
            MapButton.Foreground = Brushes.White;
        }
        private void EditorDiscardButton_Click(object sender, RoutedEventArgs e)
        {
            EditorManager.Editor?.Clear();
            EditorQuitWarning.Visibility = Visibility.Hidden;
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
        public void ResetTransforms()
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
            if (GameManager == null) // leftover objects
            {
                GameManager = new(new(WorldCanvas));
                GameManager.HUD = GameHUD;
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
            if (EditorManager == null) // leftover objects
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

        private void InitializeMode(IManager mode)
        {
            if (CurrentMode == mode) return;
            CurrentMode = mode;
            StartMenu.Visibility = Visibility.Hidden;
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
                    RuntimeDebugText.Text = $"fps:{QOL.GetAverageFPS(dt):F0}";
                }
            }
        }
        private void Pause() => CurrentMode.Active = Active = false;
        private void Resume() => CurrentMode.Active = Active = true;
        private void Exit()
        {
            DebugManager.Clear(); //!!!!
            if (CurrentMode == null) return;
            CompositionTarget.Rendering -= OnRender;
            CurrentMode.End();

            if (CurrentMode is GameManager) DisposeGameManager();
            if (CurrentMode is EditorManager) DisposeEditorManager();

            Map = null;
            WorldCanvas.Children.Clear();
            ResetTransforms();

            GameCanvas.Visibility = Visibility.Hidden;
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
    }
}