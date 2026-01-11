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
        private double lastTime;

        private bool WindowDragging = false;
        private Point WindowDragOffset;

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

        private readonly HashSet<Key> PressedKeys = [];

        private Map Map;
        private readonly Size DefaultMapSize = new(12800, 12800);

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
        private void EscapeButtonMenu_Click(object sender, RoutedEventArgs e) => Escape();
        private void ClosingButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private HashSet<Key> IgnoredKeys = [Key.LeftAlt, Key.RightAlt, Key.Tab, Key.Capital];
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IgnoredKeys.Contains(e.Key))
                e.Handled = true;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.IsRepeat)
                PressedKeys.Add(e.Key);
        }
        private void Window_KeyUp(object sender, KeyEventArgs e) => PressedKeys.Remove(e.Key);

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
            if (int.TryParse(MapWidthContent.Text, out int width)
                && width >= 100
                && width <= 20000
                && int.TryParse(MapHeightContent.Text, out int height)
                && height >= 100
                && height <= 20000)
            {
                ResizeMap(new(width, height));
                MapMenu.Visibility = Visibility.Hidden;
                StartMenu.Visibility = Visibility.Visible;
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
        private void CreateMap(Size size)
        {
            Map = new(size);
            AddElementToCanvas(Map, 0);
            MapButton.Foreground = Brushes.White;
        }
        private void ResizeMap(Size size)
        {
            double w = size.Width;
            double h = size.Height;

            if (Map != null)
            {
                Map.Width = w;
                Map.Height = h;
            }
            else CreateMap(size);

            GameManager?.SpatialCellGrid?.Width = w;
            GameManager?.SpatialCellGrid?.Height = h;
            GameManager?.SpatialCellGrid?.Update();

            EditorManager.EditorGrid?.Width = w;
            EditorManager.EditorGrid?.Height = h;
            EditorManager.EditorGrid?.Update();
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

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameManager == null) // leftover objects
            {
                GameManager = new(new(WorldCanvas));
                GameManager.HUD = GameHUD;
                GameManager.AddElementToCanvas += AddElementToCanvas;
                GameManager.ClearViewport += ClearViewport;
                GameManager.ClearViewport += ClearMap;
            }
            InitializeMode(GameManager);
        }
        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditorManager == null) // leftover objects
            {
                EditorManager = new();
                EditorManager.HUD = EditorHUD;
                EditorManager.AddElementToCanvas += AddElementToCanvas;
                EditorManager.ResizeMap += ResizeMap;
            }
            InitializeMode(EditorManager);
        }

        public MainWindow()
        {
            InitializeComponent();

            lastTime = Uptime.Elapsed.TotalSeconds;

            TitleText.Foreground = QOL.RandomColor();
            TitleTextShadow.Foreground = QOL.RandomColor();

            MapWidthContent.Text = DefaultMapSize.Width.ToString();
            MapHeightContent.Text = DefaultMapSize.Height.ToString();
        }

        private void InitializeMode(IManager mode)
        {
            if (CurrentMode == mode) return;
            CurrentMode?.End();

            CurrentMode = mode;
            StartMenu.Visibility = Visibility.Hidden;
            StartMode();
        }

        private void StartMode()
        {
            if (CurrentMode == null) return;

            GameCanvas.Visibility = Visibility.Visible;
            if (Map == null) CreateMap(DefaultMapSize);

            Context ??= new()
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
            CurrentMode.Start();

            CompositionTarget.Rendering -= OnRender;
            CompositionTarget.Rendering += OnRender;
        }
        private void UpdateMode(double dt)
        {
            if (CurrentMode != null)
            {
                CurrentMode.Update(dt, Viewport);
                DebugText.Text = CurrentMode.ModeDebug.ToString();
            }
            Runtime(dt);
        }

        private void StopMode()
        {
            if (CurrentMode == null) return;
            CurrentMode.Stop();
        }
        private void EndMode()
        {
            if (CurrentMode == null) return;
            CurrentMode.End();
        }

        private void Runtime(double dt)
        {
            DebugHelper.Uptime = Uptime.Elapsed;
            DebugHelper.FramesPerSecond = QOL.GetAverageFPS(dt);
            DebugHelper.DeltaTime = dt;
            //sb.AppendLine($"upt:{Uptime:hh\\:mm\\:ss}");
            //sb.AppendLine($"fps:{FramesPerSecond:F0}");
            //sb.Append($"dt:{DeltaTime:F3}");
            RuntimeDebugText.Text = new StringBuilder($"upt:{Uptime.Elapsed:hh\\:mm\\:ss}|fps:{QOL.GetAverageFPS(dt):F0}|dt:{dt:F3}").ToString();
        }
        private double CurrentFrame()
        {
            double now = Uptime.Elapsed.TotalSeconds;
            double dt = Math.Min(now - lastTime, 0.05);
            lastTime = now;
            return dt;
        }
        private void OnRender(object sender, EventArgs e) => UpdateMode(CurrentFrame());

        private void Escape()
        {

        }
    }
}