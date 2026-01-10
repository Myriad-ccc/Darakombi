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
        private int ticks;

        private GameManager GameManager;
        private EditorManager EditorManager;
        private IManager Current;

        private GlobalContext Context;

        private Rect Viewport
        {
            get
            {
                var scaleX = CameraScale.ScaleX == 0 ? 1 : CameraScale.ScaleX;
                var scaleY = CameraScale.ScaleY == 0 ? 1 : CameraScale.ScaleY;
                return new(
                    -CameraTransform.X,
                    -CameraTransform.Y,
                    ActualWidth / scaleX,
                    ActualHeight / scaleY);
            }
        }

        private Point CameraCenter => new(
            ActualWidth / (CameraScale.ScaleX * 2) - CameraTransform.X,
            ActualHeight / (CameraScale.ScaleY * 2) - CameraTransform.Y);

        private readonly HashSet<Key> PressedKeys = [];
        private Map Map;
        private readonly Size DefaultMapSize = new(12800, 12800);

        private Player Player => GameManager?.Player;
        private SpatialGrid SpatialGrid => GameManager?.SpatialGrid;
        private Editor Editor => EditorManager?.Editor;

        private StringBuilder dynamicDebugInfo = new();
        private StringBuilder eventDebugInfo = new();
        private StringBuilder entityCounter = new();

        private const int GameCellSize = 128;
        private const int EditorCellSize = 64;

        private Brush EditorTileColor => RedSliderText.Foreground;

        private enum Menu
        {
            Start,
            Game,
            Editor,
        }

        private Menu CurrentMenu = Menu.Start;
        private Stack<Menu> BackMenus = new();
        private Stack<Menu> ForwardMenus = new();

        private void EscapeButtonMenu_Click(object sender, RoutedEventArgs e) => Escape();

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        private void TitleText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                TitleTextShadow.Foreground = QOL.RandomColor();
                TitleText.Foreground = QOL.RandomColor();
            }
        }
        private void ClosingButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.SystemKey is Key.LeftAlt || e.SystemKey is Key.RightAlt)
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
            if (CurrentMenu == Menu.Editor)
            {
                EditorManager.OnMouseDown(sender, e, EditorTileColor);
            }
        }
        private void GameCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentMenu == Menu.Editor) EditorManager.OnMouseUp(sender, e);
        }
        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (CurrentMenu == Menu.Editor) EditorManager.OnMouseMove(sender, e, EditorTileColor);
        }
        private void GameCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CurrentMenu == Menu.Editor) EditorManager.OnMouseWheel(sender, e);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e) { GoToMenu(Menu.Game); Start(); }
        private void EditorButton_Click(object sender, RoutedEventArgs e) { GoToMenu(Menu.Editor); Start(); }

        private void GhostTitle_Click(object sender, RoutedEventArgs e)
        {
            //if (Player == null) return;
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
            //if(GameManager.Renderer == null) return;
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
                GoToMenu(Menu.Editor);
                Start();
                EditorManager.Editor?.Clear();
                EditorManager.Editor?.Load(dialog.FileName);
            }
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
            GoToPreviousMenu();
        }

        private void RedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateEditorColor();
        private void GreenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateEditorColor();
        private void BlueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateEditorColor();
        private void UpdateEditorColor()
        {
            if (RedSlider == null || GreenSlider == null || BlueSlider == null
                || RedSliderText == null || GreenSliderText == null || BlueSliderText == null) return;
            var color = new SolidColorBrush(Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value));
            RedSliderText.Foreground = color;
            GreenSliderText.Foreground = color;
            BlueSliderText.Foreground = color;
        }

        public MainWindow()
        {
            InitializeComponent();

            lastTime = Uptime.Elapsed.TotalSeconds;

            TitleText.Foreground = QOL.RandomColor();
            TitleTextShadow.Foreground = QOL.RandomColor();

            MapWidthContent.Text = "12800";
            MapHeightContent.Text = "12800";
        }

        private void UpdateMenu()
        {
            GameCanvas.Visibility = Visibility.Hidden;
            StartMenu?.Visibility = Visibility.Hidden;
            MapMenu?.Visibility = Visibility.Hidden;
            GameHUD?.Visibility = Visibility.Hidden;
            EditorHUD?.Visibility = Visibility.Hidden;

            switch (CurrentMenu)
            {
                case Menu.Start:
                    StartMenu?.Visibility = Visibility.Visible;
                    break;
                case Menu.Game:
                    GameHUD?.Visibility = Visibility.Visible;
                    break;
                case Menu.Editor:
                    EditorHUD?.Visibility = Visibility.Visible;
                    break;
            }
            QOL.D($"Switched to {CurrentMenu}");
        }
        private void GoToMenu(Menu newMenu)
        {
            if (newMenu == CurrentMenu) return;

            BackMenus.Push(CurrentMenu);
            ForwardMenus.Clear();

            CurrentMenu = newMenu;
            UpdateMenu();
        }
        private void GoToPreviousMenu()
        {
            if (BackMenus.Count == 0) return;

            ClearMenuBeforeSwitching();

            ForwardMenus.Push(CurrentMenu);
            CurrentMenu = BackMenus.Pop();
            UpdateMenu();
        }
        private void GoToNextMenu()
        {
            if (ForwardMenus.Count == 0) return;

            ClearMenuBeforeSwitching();

            BackMenus.Push(CurrentMenu);
            CurrentMenu = ForwardMenus.Pop();
            UpdateMenu();
        }
        private void ClearMenuBeforeSwitching()
        {
            switch (CurrentMenu)
            {
                case Menu.Game:
                    //EndGame();
                    break;
                case Menu.Editor:
                    if (Editor.Tiles.Count > 0 && !EditorManager.Editor.Saved)
                    {
                        EditorQuitWarning.Visibility = Visibility.Visible;
                    }
                    break;
            }
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
            else
                CreateMap(size);

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

        private void StartGame()
        {
            GameManager = new(Context, new(WorldCanvas));
            GameManager.AddElementToCanvas += AddElementToCanvas;
            GameManager.ClearViewport += ClearViewport;
            GameManager.ClearViewport += ClearMap;
            GameManager.Start();
        }
        private void StartEditor()
        {
            EditorManager = new(Context, GameCanvas);
            EditorManager.AddElementToCanvas += AddElementToCanvas;
            EditorManager.ResizeMap += ResizeMap;
            EditorManager.Start();
        }
        private void Start()
        {
            GameCanvas.Visibility = Visibility.Visible;
            if (Map == null) CreateMap(DefaultMapSize);
            Context ??= new()
            {
                Viewport = Viewport,
                Map = Map,
                PressedKeys = PressedKeys,
                ScaleTransform = CameraScale,
                TranslateTransform = CameraTransform
            };

            if (CurrentMenu == Menu.Game) StartGame();
            else if (CurrentMenu == Menu.Editor) StartEditor();

            CompositionTarget.Rendering += OnRender;
        }

        private double CurrentFrame()
        {
            double now = Uptime.Elapsed.TotalSeconds;
            double dt = Math.Min(now - lastTime, 0.05);
            lastTime = now;
            ticks = ++ticks % 100;
            return dt;
        }
        private void Update(double dt)
        {
            switch (CurrentMenu) // switch to Current later
            {
                case Menu.Game:
                    GameManager.Update(dt, Viewport);
                    DebugText.Text = $"{GameManager.EventDebug}\n{GameManager.DynamicDebug}";
                    break;
                case Menu.Editor:
                    EditorManager.Update(dt, Viewport);
                    DebugText.Text = EditorManager.EventDebug.ToString();
                    break;
            }
            GlobalDebug(dt);
        }
        private void GlobalDebug(double dt)
        {
            DebugHelper.FramesPerSecond = QOL.GetAverageFPS(dt);
            DebugHelper.DeltaTime = dt;
            DebugGlobalText.Text = DebugHelper.GetApp();
        }
        private void OnRender(object sender, EventArgs e) => Update(CurrentFrame());

        private void Escape()
        {

        }
    }
}