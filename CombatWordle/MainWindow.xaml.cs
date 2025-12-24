global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Input;
global using System.Windows.Media;
using System.Windows.Threading;

namespace CombatWordle
{
    public partial class MainWindow : Window
    {
        private bool FormDragging = false;
        private Point DragOffset;
        private readonly HashSet<Key> PressedKeys = [];

        private GameState gameState;
        private Player player;

        private void Form_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                FormDragging = true;

                var mouseLoc = PointToScreen(e.GetPosition(this));
                DragOffset = new Point(mouseLoc.X - Left, mouseLoc.Y - Top);
                Mouse.Capture((UIElement)sender);
            }
        }
        private void Form_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                FormDragging = false;
                Mouse.Capture(null);
            }
        }
        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (FormDragging)
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
        private void QuitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void FirstModeButton_Click(object sender, RoutedEventArgs e)
        {
            StartFirstMode();
        }

        private async void StartFirstMode()
        {
            StartMenu.Visibility = Visibility.Hidden;
            GameCanvas.Visibility = Visibility.Visible;
            await RunGame();
        }

        bool canCheck = true;

        public MainWindow()
        {
            InitializeComponent();

            TitleText.Foreground = QOL.RandomColor();
            TitleTextShadow.Foreground = QOL.RandomColor();

            gameState = new GameState();
            var map = new Border()
            {
                Width = gameState.MapWidth,
                Height = gameState.MapHeight,
                Background = QOL.RGB(20),
                BorderBrush = QOL.RGB(15),
                BorderThickness = new Thickness(50)
            };
            GameCanvas.Children.Add(map);
            player = gameState.Player;
            GameCanvas.Children.Add(player.Visual);

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, ev) =>
            {
                canCheck = true;
            };
            timer.Start();

            StartFirstMode();
        }

        private void PlayerMovement()
        {
            double dx = 0;
            double dy = 0;

            if (PressedKeys.Contains(Key.W)) dy -= player.Speed;
            if (PressedKeys.Contains(Key.A)) dx -= player.Speed;
            if (PressedKeys.Contains(Key.S)) dy += player.Speed;
            if (PressedKeys.Contains(Key.D)) dx += player.Speed;

            if (dx != 0 || dy != 0)
            {
                double totalVectorLength = Math.Sqrt(dx * dx + dy * dy);

                dx = dx / totalVectorLength * player.Speed;
                dy = dy / totalVectorLength * player.Speed;
            }

            gameState.Player.WorldPos.Move(dx, dy);

            Canvas.SetLeft(gameState.Player.Visual, gameState.Player.WorldPos.X);
            Canvas.SetTop(gameState.Player.Visual, gameState.Player.WorldPos.Y);

            if (PressedKeys.Contains(Key.K) && canCheck)
            {
                QOL.WriteOut($"{player.WorldPos.X:F0}, {player.WorldPos.Y:F0}");
                canCheck = false;
                PressedKeys.Remove(Key.K);
            }
        }

        private void CameraMovement()
        {
            double px = player.WorldPos.X + player.Width / 2;
            double py = player.WorldPos.Y + player.Height / 2;

            double screenCenterX = ActualWidth / 2;
            double screenCenterY = ActualHeight / 2;

            double offsetX = screenCenterX - px;
            double offsetY = screenCenterY - py;

            offsetX = Math.Min(0, Math.Max(offsetX, ActualWidth - gameState.MapWidth));
            offsetY = Math.Min(0, Math.Max(offsetY, ActualHeight - gameState.MapHeight));

            CameraTransform.X = offsetX;
            CameraTransform.Y = offsetY;
        }

        private async Task GameLoop()
        {
            while (!gameState.GameOver)
            {
                PlayerMovement();
                CameraMovement();
                await Task.Delay(16);
            }
        }

        private async Task RunGame()
        {
            await GameLoop();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) => PressedKeys.Add(e.Key);
        private void Window_KeyUp(object sender, KeyEventArgs e) => PressedKeys.Remove(e.Key);
    }
}