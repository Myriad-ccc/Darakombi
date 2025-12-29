global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Text;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Input;
global using System.Windows.Media;
using System.Windows.Shapes;

namespace CombatWordle
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch Uptime = Stopwatch.StartNew();
        private double lastTime;

        private Rect Viewport => new(-CameraTransform.X, -CameraTransform.Y, ActualWidth, ActualHeight);

        private bool WindowDragging = false;
        private Point DragOffset;
        private readonly HashSet<Key> PressedKeys = [];

        private GameState game;
        private SpatialGrid spatialGrid;
        private SceneManager sceneManager;

        private List<EntityData> visible = [];
        private List<EntityData> hidden = [];

        private Map map => game.Map;
        private Player player => game.Player;

        private StringBuilder debugInfo = new();
        private StringBuilder entityCounter = new();

        private void Form_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                WindowDragging = true;

                var mouseLoc = PointToScreen(e.GetPosition(this));
                DragOffset = new Point(mouseLoc.X - Left, mouseLoc.Y - Top);
                Mouse.Capture((UIElement)sender);
            }
        }
        private void Form_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                WindowDragging = false;
                Mouse.Capture(null);
            }
        }
        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (WindowDragging)
                {
                    var screenLoc = PointToScreen(e.GetPosition(this));

                    Left = screenLoc.X - DragOffset.X;
                    Top = screenLoc.Y - DragOffset.Y;
                }
            }
        }
        private void TitleText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                if (sender is TextBlock tb)
                    tb.Foreground = QOL.RandomColor();
            }
            else if (e.ChangedButton == MouseButton.Left)
                Form_MouseDown(sender, e);
        }
        private void ClosingButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void PlayButton_Click(object sender, RoutedEventArgs e) => StartGame();
        private void DebugText_MouseUp(object sender, MouseButtonEventArgs e) => DebugText.Visibility = e.ChangedButton == MouseButton.Right ? Visibility.Hidden : DebugText.Visibility;
        private void Window_KeyDown(object sender, KeyEventArgs e) => PressedKeys.Add(e.Key);
        private void Window_KeyUp(object sender, KeyEventArgs e) => PressedKeys.Remove(e.Key);

        public MainWindow()
        {
            InitializeComponent();

            lastTime = Uptime.Elapsed.TotalSeconds;

            TitleText.Foreground = QOL.RandomColor();
            TitleTextShadow.Foreground = QOL.RandomColor();

            game = new GameState();
            spatialGrid = new(map.Width, map.Height);
            sceneManager = new(GameCanvas, visible, hidden);

            GameCanvas.Children.Add(map);
            Canvas.SetLeft(map, 0);
            Canvas.SetTop(map, 0);

            DrawSpatialGrid();

            StartGame();
        }

        private void DrawSpatialGrid()
        {
            var grid = new Grid()
            {
                IsHitTestVisible = false,
                Opacity = 0.6
            };
            GameCanvas.Children.Add(grid);

            int cellSize = 128;
            double width = map.Width;
            double height = map.Height;

            var brush = Brushes.Green;

            for (int x = 0; x <= width; x += cellSize)
            {
                var line = new Line()
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = height,
                    Stroke = brush,
                    StrokeThickness = 1
                };
                grid.Children.Add(line);
            }

            for (int y = 0; y <= height; y += cellSize)
            {
                var line = new Line()
                {
                    X1 = 0,
                    X2 = width,
                    Y1 = y,
                    Y2 = y,
                    Stroke = brush,
                    StrokeThickness = 1
                };
                grid.Children.Add(line);
            }
        }

        private void PlayerMovement(double dt)
        {
            double dx = 0;
            double dy = 0;

            if (PressedKeys.Contains(Key.W)) dy -= 1;
            if (PressedKeys.Contains(Key.A)) dx -= 1;
            if (PressedKeys.Contains(Key.S)) dy += 1;
            if (PressedKeys.Contains(Key.D)) dx += 1;

            if (dx != 0 || dy != 0)
            {
                double totalVectorLength = Math.Sqrt(dx * dx + dy * dy);

                dx /= totalVectorLength;
                dy /= totalVectorLength;
            }

            dx *= player.Speed * dt;
            dy *= player.Speed * dt;

            Point pos = player.WorldPos;
            Size size = player.Size;
            Rect newRect;

            double gap = 1e-10;
            double leftEdge = map.Thickness;
            double topEdge = map.Thickness;
            double rightEdge = map.Width - map.Thickness - player.Width;
            double bottomEdge = map.Height - map.Thickness - player.Height;

            Rect searchArea = player.Rect;
            searchArea.Inflate(player.Speed * dt + 10, player.Speed * dt + 10);
            var colliders = spatialGrid.Search(searchArea);

            pos.X += dx;
            newRect = new Rect(pos, size);
            foreach (var collider in colliders.Where(c => c.Entity.CollisionType != CollisionType.Live))
            {
                if (newRect.IntersectsWith(collider.Rect))
                {
                    if (dx > 0)
                        pos.X = collider.Pos.X - player.Width - gap;
                    else if (dx < 0)
                        pos.X = collider.Pos.X + collider.Width + gap;
                    newRect = new Rect(pos, size);
                }
            }

            pos.Y += dy;
            newRect = new Rect(pos, size);
            foreach (var collider in colliders.Where(c => c.Entity.CollisionType != CollisionType.Live))
            {
                if (newRect.IntersectsWith(collider.Rect))
                {
                    if (dy > 0)
                        pos.Y = collider.Pos.Y - player.Height - gap;
                    else if (dy < 0)
                        pos.Y = collider.Pos.Y + collider.Height + gap;
                    newRect = new Rect(pos, size);
                }
            }

            pos.X = Math.Max(leftEdge, Math.Min(pos.X, rightEdge));
            pos.Y = Math.Max(topEdge, Math.Min(pos.Y, bottomEdge));

            Debug.Assert(
                !double.IsNaN(pos.X)
                && !double.IsNaN(pos.Y));

            player.WorldPos = pos;

            debugInfo.AppendLine($"dx:{dx:F1}\ndy:{dy:F1}");
            debugInfo.AppendLine($"vx:{dx * 1 / dt:F1}\nvy:{dy * 1 / dt:F1}");
        }

        private void CameraMovement()
        {
            double px = player.WorldPos.X + player.Width / 2;
            double py = player.WorldPos.Y + player.Height / 2;

            double screenCenterX = ActualWidth / 2;
            double screenCenterY = ActualHeight / 2;

            double offsetX = screenCenterX - px;
            double offsetY = screenCenterY - py;

            offsetX = Math.Min(0, Math.Max(offsetX, ActualWidth - game.Map.Width));
            offsetY = Math.Min(0, Math.Max(offsetY, ActualHeight - game.Map.Height));

            CameraTransform.X = offsetX;
            CameraTransform.Y = offsetY;

            debugInfo.AppendLine($"px:{px:F1}\npy:{py:F1}");
        }

        public void Move(double dt)
        {
            PlayerMovement(dt);
            CameraMovement();
        }

        private void HandleHotKeys()
        {
            if (PressedKeys.Remove(Key.R))
                game.AddTestRock();
            if (PressedKeys.Remove(Key.G))
                game.PopulateMap<Rock>(2000);
        }

        private void DebugGo(double dt)
        {
            debugInfo.AppendLine($"fps:{QOL.GetAverageFPS(dt):F0}");
            debugInfo.AppendLine($"dt:{dt:F3}");
            entityCounter.AppendLine("Entities:");
            entityCounter.AppendLine($"players:{game.Players.Count}");
            entityCounter.AppendLine($"rocks:{game.Rocks.Count}");
            DebugText.Text = debugInfo.ToString();
            EntityCounter.Text = entityCounter.ToString();
        }

        private void Update(double dt)
        {
            debugInfo.Clear();
            entityCounter.Clear();
            HandleHotKeys();
            Move(dt);
            foreach (var entityData in game.AllEntityData)
                spatialGrid.Update(entityData);
            sceneManager.Update(Viewport, game.AllEntityData);
            DebugGo(dt);
        }

        private double CurrentFrame()
        {
            double now = Uptime.Elapsed.TotalSeconds;
            double dt = Math.Min(now - lastTime, 0.05);
            lastTime = now;
            return dt;
        }

        private void OnRender(object sender, EventArgs e)
        {
            Update(CurrentFrame());
        }

        private async void StartGame()
        {
            StartMenu.Visibility = Visibility.Hidden;
            GameCanvas.Visibility = Visibility.Visible;

            CompositionTarget.Rendering += OnRender;
        }
    }
}