global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Text;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Input;
global using System.Windows.Media;

namespace CombatWordle
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch Uptime = Stopwatch.StartNew();
        private double lastTime;

        private Rect Viewport
        {
            get
            {
                var scaleX = CameraScale.ScaleX == 0 ? 1 : CameraScale.ScaleX;
                return new(
                    -CameraTransform.X,
                    -CameraTransform.Y,
                    ActualWidth / scaleX,
                    ActualHeight / scaleX);
            }
        }
        private Rect ViewportPlus
        {
            get
            {
                var temp = Viewport;
                temp.Inflate(100, 100); //originally 150, 150
                return temp;
            }
        }

        private Point CameraCenter => new(
            ActualWidth / (CameraScale.ScaleX * 2) - CameraTransform.X,
            ActualHeight / (CameraScale.ScaleY * 2) - CameraTransform.Y);

        private readonly HashSet<Key> PressedKeys = [];
        private bool DraggingCamera;
        private Point LastMousePos;

        private GameState game;
        private Editor editor;
        private SceneManager sceneManager;

        private Map map => game.Map;
        private SpatialGrid grid => game.spatialGrid;
        private Player player => game.Player;

        private StringBuilder debugInfo = new();
        private StringBuilder entityCounter = new();
        private Point ActiveMousePos = new();

        private bool gameMode = false;
        private bool editorMode = false;

        private const int cellSize = 128;
        private GridHelper cellGrid;
        private const int editorCellSize = 64;
        private GridHelper editorGrid;

        private void UpdateEditorColor()
        {
            if (RedSlider == null || GreenSlider == null || BlueSlider == null
                || RedSliderText == null || GreenSliderText == null || BlueSliderText == null) return;
            var color = new SolidColorBrush(Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value));
            RedSliderText.Foreground = color;
            GreenSliderText.Foreground = color;
            BlueSliderText.Foreground = color;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
            if (e.ChangedButton == MouseButton.Right)
            {
                TitleText.Foreground = QOL.RandomColor();
                TitleTextShadow.Foreground = QOL.RandomColor();
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e) => PressedKeys.Add(e.Key);
        private void Window_KeyUp(object sender, KeyEventArgs e) => PressedKeys.Remove(e.Key);

        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (editorMode)
                {
                    QOL.D("Started dragging camera");
                    DraggingCamera = true;
                    LastMousePos = e.GetPosition(this);
                    Mouse.Capture((UIElement)sender);
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (editorMode)
                {
                    EditorPlace(e.GetPosition((UIElement)GameCanvas.Parent));
                }
            }
        }
        private void GameCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (editorMode && DraggingCamera)
                {
                    QOL.D("Stopped dragging camera");
                    var currentPos = e.GetPosition(this);

                    double dx = currentPos.X - LastMousePos.X;
                    double dy = currentPos.Y - LastMousePos.Y;

                    CameraTransform.X += dx;
                    CameraTransform.Y += dy;

                    LastMousePos = currentPos;
                }
                if (Mouse.Captured == sender)
                    Mouse.Capture(null);
            }
        }
        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var temp = e.GetPosition((UIElement)GameCanvas.Parent);
            ActiveMousePos = new(temp.X / CameraScale.ScaleX - CameraTransform.X, temp.Y / CameraScale.ScaleY - CameraTransform.Y);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (editorMode && DraggingCamera)
                {
                    QOL.D("Moving camera");
                    var currentPos = e.GetPosition(this);
                    double dx = currentPos.X - LastMousePos.X;
                    double dy = currentPos.Y - LastMousePos.Y;

                    CameraTransform.X += dx / CameraScale.ScaleX;
                    CameraTransform.Y += dy / CameraScale.ScaleY;

                    LastMousePos = currentPos;
                }
            }
        }

        private void GameCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!editorMode) return;
            double zoomIn = 1.1;
            double factor = Math.Pow(zoomIn, e.Delta / 120.0);
            double newScale = Math.Max(0.35, Math.Min(CameraScale.ScaleX * factor, 5.0));

            CameraScale.ScaleX = newScale;
            CameraScale.ScaleY = newScale;
        }

        private void TitleText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                if (sender is TextBlock tb)
                    tb.Foreground = QOL.RandomColor();
            }
            else if (e.ChangedButton == MouseButton.Left)
                TitleBar_MouseDown(sender, e);
        }
        private void ClosingButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void PlayButton_Click(object sender, RoutedEventArgs e) { gameMode = true; Start(); }
        private void EditorButton_Click(object sender, RoutedEventArgs e) { editorMode = true; Start(); }

        private void GhostTitle_Click(object sender, RoutedEventArgs e)
        {
            if (player == null) return;
            bool isGhost = player.CollisionType == CollisionType.Ghost;
            if (isGhost)
            {
                player.CollisionType = CollisionType.Live;
                GhostBool.Content = "OFF";
                GhostBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                player.CollisionType = CollisionType.Ghost;
                GhostBool.Content = "ON";
                GhostBool.Foreground = Brushes.LightGreen;
            }
        }
        private void OverlayTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isShown = sceneManager.ShowOverlays;
            if (isShown)
            {
                sceneManager.ShowOverlays = false;
                OverlayBool.Content = "OFF";
                OverlayBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                sceneManager.ShowOverlays = true;
                OverlayBool.Content = "ON";
                OverlayBool.Foreground = Brushes.LightGreen;
            }
        }
        private void GridTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isShown = cellGrid.Visibility == Visibility.Visible;
            if (isShown)
            {
                cellGrid.Visibility = Visibility.Hidden;
                GridBool.Content = "OFF";
                GridBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                cellGrid.Visibility = Visibility.Visible;
                GridBool.Content = "ON";
                GridBool.Foreground = Brushes.LightGreen;
            }
        }

        private void EditorSave_Click(object sender, RoutedEventArgs e)
        {

        }
        private void EditorGridTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isShown = editorGrid.Visibility == Visibility.Visible;
            if (isShown)
            {
                editorGrid.Visibility = Visibility.Hidden;
                EditorGridBool.Content = "OFF";
                EditorGridBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                editorGrid.Visibility = Visibility.Visible;
                EditorGridBool.Content = "ON";
                EditorGridBool.Foreground = Brushes.LightGreen;
            }
        }

        private void RedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateEditorColor();
        private void GreenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateEditorColor();
        private void BlueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateEditorColor();

        public MainWindow()
        {
            InitializeComponent();

            lastTime = Uptime.Elapsed.TotalSeconds;

            TitleText.Foreground = QOL.RandomColor();
            TitleTextShadow.Foreground = QOL.RandomColor();

            game = new GameState();
            sceneManager = new(GameCanvas);

            GameCanvas.Children.Add(map);
            Canvas.SetLeft(map, 0);
            Canvas.SetTop(map, 0);
            Panel.SetZIndex(map, 0);
        }

        private void PlayerMovement(double dt)
        {
            double dx = 0;
            double dy = 0;

            if (PressedKeys.Contains(Key.W)) dy -= 1;
            if (PressedKeys.Contains(Key.A)) dx -= 1;
            if (PressedKeys.Contains(Key.S)) dy += 1;
            if (PressedKeys.Contains(Key.D)) dx += 1;

            (dx, dy) = GameState.NormalizeSpeed(dx, dy, player.Speed, dt);

            Point pos = player.Pos;
            Size size = player.Size;
            Rect newRect;

            double gap = 1e-10;
            double leftEdge = map.Thickness;
            double topEdge = map.Thickness;
            double rightEdge = map.Width - map.Thickness - player.Width;
            double bottomEdge = map.Height - map.Thickness - player.Height;

            Rect searchArea = player.Rect;
            searchArea.Inflate(player.Speed * dt + 10, player.Speed * dt + 10);
            var colliders = grid.Search(searchArea);

            pos.X += dx;
            newRect = new Rect(pos, size);
            foreach (var collider in colliders.Where(c => c.Entity.CollisionType != CollisionType.Live))
            {
                if (player.CollisionType == CollisionType.Ghost) break;
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
                if (player.CollisionType == CollisionType.Ghost) break;
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

            player.Pos = pos;

            debugInfo.AppendLine($"dx:{dx:F1}\ndy:{dy:F1}");
            debugInfo.AppendLine($"vx:{dx * 1 / dt:F1}\nvy:{dy * 1 / dt:F1}");
        }

        private void PlayerCameraMovement()
        {
            double px = player.Pos.X + player.Width / 2;
            double py = player.Pos.Y + player.Height / 2;

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
            PlayerCameraMovement();
            foreach (var enemy in game.Enemies) //cull later
                game.EnemyAI(enemy, dt);
        }

        private void GameShortcuts()
        {
            if (PressedKeys.Remove(Key.R))
                game.AddTestRock();
            if (PressedKeys.Remove(Key.G))
                game.PopulateMap<Rock>(2000);
            if (PressedKeys.Remove(Key.V))
                ClearViewport();
            if (PressedKeys.Remove(Key.M))
                ClearMap();
            if (PressedKeys.Remove(Key.E))
                game.AddTestEnemy();
        }
        private void ClearArea(Rect area)
        {
            var targets = grid.Search(area);
            var toRemove = targets.Where(e => e.Entity is not Player);

            foreach (var data in toRemove)
            {
                sceneManager.Remove(data);
                game.AllEntityData.Remove(data);
                if (data.Entity is Rock rock) game.Rocks.Remove(rock);
                game.spatialGrid.Remove(data);
            }
        }
        private void ClearViewport()
        {
            ClearArea(ViewportPlus);
        }
        private void ClearMap()
        {
            foreach (var data in game.AllEntityData)
                if (data.Entity is not Player)
                    sceneManager.Remove(data);

            game.Rocks.Clear();
            game.Entities.RemoveAll(e => e is not Player);
            game.AllEntityData.RemoveAll(e => e.Entity is not Player);
            game.spatialGrid.ClearAll();
            game.spatialGrid.Add(game.AllEntityData[0]);
            sceneManager.ClearCache();
            GC.Collect();
        }
        private void GameDebug()
        {
            entityCounter.AppendLine("Entities:");
            entityCounter.AppendLine($"rocks:{game.Rocks.Count}");
            entityCounter.AppendLine($"enemies:{game.Enemies.Count}");
            DebugText.Text = debugInfo.ToString();
            EntityCounter.Text = entityCounter.ToString();
        }

        private void EditorMove(double dt)
        {
            double pan = 500 * dt;
            if (PressedKeys.Contains(Key.W) || PressedKeys.Contains(Key.Up)) CameraTransform.Y += pan;
            if (PressedKeys.Contains(Key.A) || PressedKeys.Contains(Key.Left)) CameraTransform.X += pan;
            if (PressedKeys.Contains(Key.D) || PressedKeys.Contains(Key.Right)) CameraTransform.X -= pan;
            if (PressedKeys.Contains(Key.S) || PressedKeys.Contains(Key.Down)) CameraTransform.Y -= pan;
        }

        private void EditorDebug()
        {
            debugInfo.AppendLine($"mx:{ActiveMousePos.X:F1}");
            debugInfo.AppendLine($"my:{ActiveMousePos.Y:F1}");
            debugInfo.AppendLine($"cx:{CameraCenter.X:F1}");
            debugInfo.AppendLine($"cy:{CameraCenter.Y:F1}");
        }

        private void GlobalDebug(double dt)
        {
            debugInfo.AppendLine($"fps:{QOL.GetAverageFPS(dt):F0}");
            debugInfo.AppendLine($"dt:{dt:F3}");
            DebugText.Text = debugInfo.ToString();
        }

        private void GameUpdate(double dt)
        {
            entityCounter.Clear();
            GameShortcuts();
            Move(dt);
            foreach (var entityData in game.LiveEntities)
                grid.Update(entityData);
            GameDebug();
        }

        private void EditorUpdate(double dt)
        {
            EditorMove(dt);
            editor.Update(ViewportPlus);
            EditorDebug();
        }

        private void EditorPlace(Point screenPos)
        {
            double x = (screenPos.X / CameraScale.ScaleX) - CameraTransform.X;
            double y = (screenPos.Y / CameraScale.ScaleY) - CameraTransform.Y;

            int cellX = (int)Math.Floor(x / editorCellSize) * editorCellSize;
            int cellY = (int)Math.Floor(y / editorCellSize) * editorCellSize;

            var obj = new Editor.EditorDTO((cellX, cellY), Brushes.Bisque);
            if (editor.Add(obj))
                QOL.D($"Placed block at {cellX}, {cellY}");
            else QOL.D($"Failed to place block at {cellX}, {cellY}");
        }

        private void Update(double dt)
        {
            debugInfo.Clear();
            if (gameMode)
                GameUpdate(dt);
            if (editorMode)
                EditorUpdate(dt);
            sceneManager.UpdateGame(game.spatialGrid.Search(ViewportPlus));
            GlobalDebug(dt);
        }

        private double CurrentFrame()
        {
            double now = Uptime.Elapsed.TotalSeconds;
            double dt = Math.Min(now - lastTime, 0.05);
            lastTime = now;
            return dt;
        }

        private void OnRender(object sender, EventArgs e) => Update(CurrentFrame());

        private void Start()
        {
            StartMenu.Visibility = Visibility.Hidden;
            GameCanvas.Visibility = Visibility.Visible;
            DebugText.Visibility = Visibility.Visible;

            if (gameMode)
            {
                GameMode();
            }
            else if (editorMode)
            {
                EditorMode();
            }

            CompositionTarget.Rendering += OnRender;
        }

        private void GameMode()
        {
            game.AddPlayer();
            cellGrid = new GridHelper(cellSize, (int)map.Width, (int)map.Height, Brushes.Green, 0.5);
            GameCanvas.Children.Add(cellGrid);
            Panel.SetZIndex(cellGrid, 10);

            EntityCounter.Visibility = Visibility.Visible;
            GameOverlay.Visibility = Visibility.Visible;
        }

        private void EditorMode()
        {
            editor = new(editorCellSize);
            GameCanvas.Children.Add(editor);

            editorGrid = new GridHelper(editorCellSize, (int)map.Width, (int)map.Height, Brushes.White, 0.05);
            GameCanvas.Children.Add(editorGrid);
            Panel.SetZIndex(editorGrid, 10);

            CameraTransform.X = CameraTransform.X / CameraScale.ScaleX - map.Center.X + ActualWidth / 2;
            CameraTransform.Y = CameraTransform.Y / CameraScale.ScaleY - map.Center.Y + ActualHeight / 2;

            EditorSave.Visibility = Visibility.Visible;
            EditorOverlay.Visibility = Visibility.Visible;
        }
    }
}