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
                temp.Inflate(100, 100);
                return temp;
            }
        }

        private Point CameraCenter => new(
            ActualWidth / (CameraScale.ScaleX * 2) - CameraTransform.X,
            ActualHeight / (CameraScale.ScaleY * 2) - CameraTransform.Y);

        private readonly HashSet<Key> PressedKeys = [];
        private bool DraggingCamera;
        private Point LastMousePos;

        private Point LastEOPos;

        private Map Map;
        private GameState game;
        private Editor editor;
        private SceneManager sceneManager;

        private SpatialGrid grid => game.spatialGrid;
        private Player player => game.Player;

        private StringBuilder dynamicDebugInfo = new();
        private StringBuilder eventDebugInfo = new();
        private StringBuilder entityCounter = new();
        private Point ActiveMousePos = new();

        private const int GameCellSize = 128;
        private GridHelper GameCellGrid;
        private const int EditorCellSize = 64;
        private GridHelper EditorGrid;

        private bool editorDrawing = false;
        private bool editorSaved = false;
        private bool editorUpdate = false;

        private enum Menu
        {
            Start,
            Game,
            Editor,
        }

        private Menu CurrentMenu = Menu.Start;
        private Stack<Menu> BackMenus = new();
        private Stack<Menu> ForwardMenus = new();

        private void UpdateMenu()
        {
            GameCanvas.Visibility = Visibility.Hidden;
            StartMenu?.Visibility = Visibility.Hidden;
            MapMenu?.Visibility = Visibility.Hidden;
            GameMenu?.Visibility = Visibility.Hidden;
            EditorMenu?.Visibility = Visibility.Hidden;

            switch (CurrentMenu)
            {
                case Menu.Start:
                    StartMenu?.Visibility = Visibility.Visible;
                    break;
                case Menu.Game:
                    GameMenu?.Visibility = Visibility.Visible;
                    break;
                case Menu.Editor:
                    EditorMenu?.Visibility = Visibility.Visible;
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
                case Menu.Editor:
                    if (editor.Tiles.Count > 0 && !editorSaved)
                    {
                        EditorQuitWarning.Visibility = Visibility.Visible;
                    }
                    break;
            }
        }

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
        private void BackButton_Click(object sender, RoutedEventArgs e) => GoToPreviousMenu();
        private void ForwardButton_Click(object sender, RoutedEventArgs e) => GoToNextMenu();

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

        private Point ScreenToWorld(Point pos) =>
            new(
                (pos.X / CameraScale.ScaleX) - CameraTransform.X,
                (pos.Y / CameraScale.ScaleY) - CameraTransform.Y);

        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CurrentMenu == Menu.Editor)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    var worldPos = ScreenToWorld(e.GetPosition((UIElement)GameCanvas.Parent));
                    EditorDrawTile(worldPos);
                    editorDrawing = true;
                    LastEOPos = worldPos;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    QOL.D("Started dragging camera");
                    DraggingCamera = true;
                    LastMousePos = e.GetPosition(this);
                    Mouse.Capture((UIElement)sender);
                }
                editorUpdate = true;
            }
        }
        private void GameCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                editorDrawing = false;
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                if (CurrentMenu == Menu.Editor && DraggingCamera)
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
            editorUpdate = true;
        }
        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var screenMousePos = e.GetPosition((UIElement)GameCanvas.Parent);
            ActiveMousePos = ScreenToWorld(screenMousePos);

            if (CurrentMenu == Menu.Editor)
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    if (DraggingCamera)
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
                else if (e.LeftButton == MouseButtonState.Pressed && editorDrawing)
                {
                    Vector direction = ActiveMousePos - LastEOPos;
                    double distance = direction.Length;
                    double step = EditorCellSize * 0.9;

                    if (distance > step)
                    {
                        direction.Normalize();
                        for (double d = 0; d < distance; d += step)
                            EditorDrawTile(LastEOPos + d * direction);
                    }
                    LastEOPos = ActiveMousePos;
                    EditorDrawTile(ActiveMousePos);
                    e.Handled = true;
                }
                editorUpdate = true;
            }
        }
        private void GameCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CurrentMenu != Menu.Editor) return;
            double zoomIn = 1.1;
            double factor = Math.Pow(zoomIn, e.Delta / 120.0);
            double newScale = Math.Max(0.1, Math.Min(CameraScale.ScaleX * factor, 5.0));

            var mousePos = e.GetPosition((UIElement)GameCanvas.Parent);
            var worldPos = ScreenToWorld(mousePos);

            CameraTransform.X = (mousePos.X / newScale) - worldPos.X;
            CameraTransform.Y = (mousePos.Y / newScale) - worldPos.Y;

            CameraScale.ScaleY = newScale;
            CameraScale.ScaleX = newScale;

            editorUpdate = true;
            DebugHelper.ZoomFactor = newScale;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e) { GoToMenu(Menu.Game); Start(); }
        private void EditorButton_Click(object sender, RoutedEventArgs e) { GoToMenu(Menu.Editor); Start(); }

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
            bool isShown = GameCellGrid.Visibility == Visibility.Visible;
            if (isShown)
            {
                GameCellGrid.Visibility = Visibility.Hidden;
                GridBool.Content = "OFF";
                GridBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                GameCellGrid.Visibility = Visibility.Visible;
                GridBool.Content = "ON";
                GridBool.Foreground = Brushes.LightGreen;
            }
        }

        private void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            EditorQuitWarning.Visibility = Visibility.Hidden;
            if (editor?.Tiles.Count > 0)
            {
                editor.Save("TestSave.txt", Map.Size);
                MessageBox.Show("saved!");
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
            if (editor.DrawOver)
            {
                DrawOverBool.Content = "OFF";
                DrawOverBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                DrawOverBool.Content = "ON";
                DrawOverBool.Foreground = Brushes.LightGreen;
            }
            editor.DrawOver = !editor.DrawOver;
        }

        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            StartMenu.Visibility = Visibility.Hidden;
            MapMenu.Visibility = Visibility.Visible;
            MapButton.Foreground = Brushes.White;
        }

        private void EditorDiscardButton_Click(object sender, RoutedEventArgs e) => editor?.Clear();
        private void EditorGridTitle_Click(object sender, RoutedEventArgs e)
        {
            bool isShown = EditorGrid.Visibility == Visibility.Visible;
            if (isShown)
            {
                EditorGrid.Visibility = Visibility.Hidden;
                EditorGridBool.Content = "OFF";
                EditorGridBool.Foreground = Brushes.IndianRed;
            }
            else
            {
                EditorGrid.Visibility = Visibility.Visible;
                EditorGridBool.Content = "ON";
                EditorGridBool.Foreground = Brushes.LightGreen;
            }
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

            sceneManager = new(GameCanvas);
        }

        private void PlayerMovement(double dt)
        {
            var d = new Vector();

            if (PressedKeys.Contains(Key.W)) d.Y -= 1;
            if (PressedKeys.Contains(Key.A)) d.X -= 1;
            if (PressedKeys.Contains(Key.S)) d.Y += 1;
            if (PressedKeys.Contains(Key.D)) d.X += 1;

            if (d.X != 0 || d.Y != 0) d.Normalize();
            d.X *= player.Speed * dt;
            d.Y *= player.Speed * dt;

            Point pos = player.Pos;
            Size size = player.Size;
            Rect newRect;

            double gap = 1e-10;
            double leftEdge = Map.Thickness;
            double topEdge = Map.Thickness;
            double rightEdge = Map.Width - Map.Thickness - player.Width;
            double bottomEdge = Map.Height - Map.Thickness - player.Height;

            Rect searchArea = player.Rect;
            searchArea.Inflate(player.Speed * dt + 10, player.Speed * dt + 10);
            var colliders = grid.Search(searchArea);

            pos.X += d.X;
            newRect = new Rect(pos, size);
            foreach (var collider in colliders.Where(c => c.Entity.CollisionType != CollisionType.Live))
            {
                if (player.CollisionType == CollisionType.Ghost) break;
                if (newRect.IntersectsWith(collider.Rect))
                {
                    if (d.X > 0)
                        pos.X = collider.Pos.X - player.Width - gap;
                    else if (d.X < 0)
                        pos.X = collider.Pos.X + collider.Width + gap;
                    newRect = new Rect(pos, size);
                }
            }

            pos.Y += d.Y;
            newRect = new Rect(pos, size);
            foreach (var collider in colliders.Where(c => c.Entity.CollisionType != CollisionType.Live))
            {
                if (player.CollisionType == CollisionType.Ghost) break;
                if (newRect.IntersectsWith(collider.Rect))
                {
                    if (d.Y > 0)
                        pos.Y = collider.Pos.Y - player.Height - gap;
                    else if (d.Y < 0)
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

            DebugHelper.DeltaX = d.X;
            DebugHelper.DeltaY = d.Y;
            DebugHelper.VelocityX = d.X * 1 / dt;
            DebugHelper.VelocityY = d.Y * 1 / dt;
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

            DebugHelper.PlayerX = px;
            DebugHelper.PlayerY = py;
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
        private void GameDebug()
        {
            entityCounter.AppendLine("Entities:");
            entityCounter.AppendLine($"rocks:{game.Rocks.Count}");
            entityCounter.AppendLine($"enemies:{game.Enemies.Count}");
            EntityCounter.Text = entityCounter.ToString();
            DebugText.Text = new StringBuilder(DebugHelper.GetGameEvent() + '\n' + DebugHelper.GetGameDynamic()).ToString();
        }

        private void EditorMove(double dt)
        {
            double pan = 1000 * dt;
            if (PressedKeys.Contains(Key.W) || PressedKeys.Contains(Key.Up)) { CameraTransform.Y += pan; editorUpdate = true; }
            if (PressedKeys.Contains(Key.A) || PressedKeys.Contains(Key.Left)) { CameraTransform.X += pan; editorUpdate = true; }
            if (PressedKeys.Contains(Key.D) || PressedKeys.Contains(Key.Right)) { CameraTransform.X -= pan; editorUpdate = true; }
            if (PressedKeys.Contains(Key.S) || PressedKeys.Contains(Key.Down)) { CameraTransform.Y -= pan; editorUpdate = true; }
        }

        private void EditorDebug()
        {
            DebugHelper.MousePosX = ActiveMousePos.X;
            DebugHelper.MousePosY = ActiveMousePos.Y;
            DebugHelper.CameraX = CameraCenter.X;
            DebugHelper.CameraY = CameraCenter.Y;
            DebugText.Text = DebugHelper.GetEditorEvent();
        }

        private void GlobalDebug(double dt)
        {
            DebugHelper.FramesPerSecond = QOL.GetAverageFPS(dt);
            DebugHelper.DeltaTime = dt;
            DebugGlobalText.Text = DebugHelper.GetApp();
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
            if (editorUpdate)
            {
                editor.Update(ViewportPlus);
                EditorDebug();
                editorUpdate = false;
            }
        }

        private void EditorDrawTile(Point worldPos)
        {
            int cellX = (int)Math.Floor(worldPos.X / EditorCellSize) * EditorCellSize;
            int cellY = (int)Math.Floor(worldPos.Y / EditorCellSize) * EditorCellSize;

            var tile = new Editor.Tile((cellX, cellY), RedSliderText.Foreground);
            editor.Add(tile);

            editorUpdate = true;
            QOL.D($"Placed block at {cellX}, {cellY}");
        }

        private void Update(double dt)
        {
            dynamicDebugInfo.Clear();
            eventDebugInfo.Clear();
            if (CurrentMenu == Menu.Game)
            {
                GameUpdate(dt);
                sceneManager.UpdateGame(game.spatialGrid.Search(ViewportPlus));
            }
            if (CurrentMenu == Menu.Editor)
                EditorUpdate(dt);
            GlobalDebug(dt);
        }

        private double CurrentFrame()
        {
            double now = Uptime.Elapsed.TotalSeconds;
            double dt = Math.Min(now - lastTime, 0.05);
            lastTime = now;
            ticks = ++ticks % 100;
            return dt;
        }

        private void OnRender(object sender, EventArgs e) => Update(CurrentFrame());

        private readonly Size DefaultMapSize = new(12800, 12800);
        private async void Start()
        {
            GameCanvas.Visibility = Visibility.Visible;
            if (Map == null) CreateMap(DefaultMapSize);

            if (CurrentMenu == Menu.Game) StartGame();
            else if (CurrentMenu == Menu.Editor) StartEditor();

            CompositionTarget.Rendering += OnRender;
        }

        private void StartGame()
        {
            game ??= new(Map);
            game.spatialGrid ??= new(Map.Width, Map.Height);
            game.AddPlayer();
            GameCellGrid ??= new GridHelper(GameCellSize, (int)Map.Width, (int)Map.Height, Brushes.Green, 0.5);
            GameCanvas.Children.Add(GameCellGrid);
            Panel.SetZIndex(GameCellGrid, 10);
        }

        private void PauseGame()
        {

        }

        public void CreateMap(Size size)
        {
            Map = new(size);
            GameCanvas.Children.Add(Map);
            Canvas.SetLeft(Map, 0);
            Canvas.SetTop(Map, 0);
            Panel.SetZIndex(Map, 0);
        }

        public void ResizeMap(Size size)
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

            GameCellGrid?.Width = w;
            GameCellGrid?.Height = h;
            GameCellGrid?.Update();

            EditorGrid?.Width = w;
            EditorGrid?.Height = h;
            EditorGrid?.Update();
        }

        private void StartEditor()
        {
            editor ??= new(EditorCellSize);
            editor.ResizeMap += ResizeMap;
            GameCanvas.Children.Add(editor);
            Canvas.SetLeft(editor, 0);
            Canvas.SetTop(editor, 0);
            Panel.SetZIndex(editor, 20);

            EditorGrid ??= new GridHelper(EditorCellSize, (int)Map.Width, (int)Map.Height, Brushes.White, 0.05);
            GameCanvas.Children.Add(EditorGrid);
            Panel.SetZIndex(EditorGrid, 10);

            ResetEditorCam();
        }

        private void ResetEditorCam()
        {
            CameraScale.ScaleX = CameraScale.ScaleY = 0.5;
            CameraTransform.X = CameraTransform.X / CameraScale.ScaleX - Map.Center.X + ActualWidth / 2;
            CameraTransform.Y = CameraTransform.Y / CameraScale.ScaleY - Map.Center.Y + ActualHeight / 2;
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
                editor.Clear();
                editor?.Load(dialog.FileName);
            }
        }
    }
}