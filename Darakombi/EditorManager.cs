namespace Darakombi
{
    public class EditorManager : IManager
    {
        public GlobalContext Context { get; set; }
        private readonly Canvas Canvas;
        public Editor Editor;

        public GridHelper EditorGrid;
        private const int EditorCellSize = 64;

        private bool DraggingCamera;
        private Point LastMousePos;
        private Point LastEOPos;
        private Point WorldMousePos = new();

        private bool Drawing = false;
        private bool NeedUpdate = false;

        public Rect Viewport => Context.Viewport;
        public Map Map => Context.Map;
        public ScaleTransform ScaleTransform => Context.ScaleTransform;
        public TranslateTransform TranslateTransform => Context.TranslateTransform;
        public HashSet<Key> PressedKeys => Context.PressedKeys;

        private Point CameraCenter => new(
            Viewport.Width / (ScaleTransform.ScaleX * 2) - TranslateTransform.X,
            Viewport.Height / (ScaleTransform.ScaleY * 2) - TranslateTransform.Y);

        public Action<UIElement, int> AddElementToCanvas;
        public Action<Size> ResizeMap;

        public StringBuilder DynamicDebug { get; set; } = new();
        public StringBuilder EventDebug { get; set; } = new();

        public EditorManager(GlobalContext context, Canvas canvas)
        {
            Context = context;
            Canvas = canvas;
        }

        public void Start()
        {
            Editor ??= new(EditorCellSize);
            Editor.ResizeMap += size => ResizeMap?.Invoke(size);
            EditorGrid ??= new GridHelper(EditorCellSize, (int)Map.Width, (int)Map.Height, Brushes.White, 0.05);
            AddElementToCanvas?.Invoke(Editor, 20);
            AddElementToCanvas?.Invoke(EditorGrid, 10);
            ResetCam();
        }

        public void Move(double dt)
        {
            double pan = 1000 * dt;
            if (PressedKeys.Contains(Key.W) || PressedKeys.Contains(Key.Up)) { TranslateTransform.Y += pan; NeedUpdate = true; }
            if (PressedKeys.Contains(Key.A) || PressedKeys.Contains(Key.Left)) { TranslateTransform.X += pan; NeedUpdate = true; }
            if (PressedKeys.Contains(Key.D) || PressedKeys.Contains(Key.Right)) { TranslateTransform.X -= pan; NeedUpdate = true; }
            if (PressedKeys.Contains(Key.S) || PressedKeys.Contains(Key.Down)) { TranslateTransform.Y -= pan; NeedUpdate = true; }
        }

        public void DrawTile(Point worldPos, Brush color)
        {
            int cellX = (int)Math.Floor(worldPos.X / EditorCellSize) * EditorCellSize;
            int cellY = (int)Math.Floor(worldPos.Y / EditorCellSize) * EditorCellSize;

            var tile = new Editor.Tile((cellX, cellY), color);
            Editor?.Add(tile);

            NeedUpdate = true;
            QOL.D($"Placed block at {cellX}, {cellY}");
        }

        public void ResetCam()
        {
            ScaleTransform.ScaleX = ScaleTransform.ScaleY = 0.5;
            TranslateTransform.X = TranslateTransform.X / ScaleTransform.ScaleX - Map.Center.X + Viewport.Width / 2;
            TranslateTransform.Y = TranslateTransform.Y / ScaleTransform.ScaleY - Map.Center.Y + Viewport.Height / 2;
        }

        public void EditorDebug()
        {
            DebugHelper.MousePosX = WorldMousePos.X;
            DebugHelper.MousePosY = WorldMousePos.Y;
            DebugHelper.CameraX = CameraCenter.X;
            DebugHelper.CameraY = CameraCenter.Y;
            EventDebug.Clear();
            EventDebug.Append(DebugHelper.GetEditorEvent());
        }

        public void Update(double dt, Rect viewport)
        {
            Context.Viewport = new(-TranslateTransform.X, -TranslateTransform.Y, viewport.Width, viewport.Height);
            Move(dt);
            if (NeedUpdate)
            {
                Editor?.Update(Viewport);
                EditorDebug();
                NeedUpdate = false;
            }
        }

        public void Stop()
        {

        }

        public void OnMouseDown(object sender, MouseButtonEventArgs e, Brush color)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var worldPos = QOL.ScreenToWorld(e.GetPosition(Canvas), TranslateTransform, ScaleTransform);
                if (Map.RectInside(worldPos))
                {
                    DrawTile(worldPos, color);
                    Drawing = true;
                    LastEOPos = worldPos;
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                QOL.D("Started dragging camera");
                DraggingCamera = true;
                LastMousePos = e.GetPosition((UIElement)Canvas.Parent);
                Mouse.Capture((UIElement)sender);
            }
            NeedUpdate = true;
        }
        public void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Drawing = false;
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                if (DraggingCamera)
                {
                    QOL.D("Stopped dragging camera");
                    var currentPos = e.GetPosition((UIElement)Canvas.Parent);

                    double dx = currentPos.X - LastMousePos.X;
                    double dy = currentPos.Y - LastMousePos.Y;

                    TranslateTransform.X += dx;
                    TranslateTransform.Y += dy;

                    LastMousePos = currentPos;
                }
                if (Mouse.Captured == sender)
                    Mouse.Capture(null);
            }
            NeedUpdate = true;
        }
        public void OnMouseMove(object sender, MouseEventArgs e, Brush color)
        {
            var screenPos = e.GetPosition(Canvas);
            WorldMousePos = QOL.ScreenToWorld(screenPos, TranslateTransform, ScaleTransform);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (DraggingCamera)
                {
                    double dx = screenPos.X - LastMousePos.X;
                    double dy = screenPos.Y - LastMousePos.Y;

                    TranslateTransform.X += dx / ScaleTransform.ScaleX;
                    TranslateTransform.Y += dy / ScaleTransform.ScaleY;

                    LastMousePos = screenPos;
                    QOL.D($"Moving camera by {dx},{dy}");
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed && Drawing && Map.RectInside(WorldMousePos))
            {
                Vector direction = WorldMousePos - LastEOPos;
                double distance = direction.Length;
                double step = EditorCellSize * 0.9;

                if (distance > step)
                {
                    direction.Normalize();
                    for (double d = 0; d < distance; d += step)
                        DrawTile(LastEOPos + d * direction, color);
                }
                LastEOPos = WorldMousePos;
                DrawTile(WorldMousePos, color);
                e.Handled = true;
            }
            NeedUpdate = true;
        }
        public void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomIn = 1.1;
            double factor = Math.Pow(zoomIn, e.Delta / 120.0);
            double newScale = Math.Max(0.1, Math.Min(ScaleTransform.ScaleX * factor, 2.0));

            var mousePos = e.GetPosition((UIElement)Canvas.Parent);
            var worldPos = QOL.ScreenToWorld(mousePos, TranslateTransform, ScaleTransform);

            TranslateTransform.X = (mousePos.X / newScale) - worldPos.X;
            TranslateTransform.Y = (mousePos.Y / newScale) - worldPos.Y;

            ScaleTransform.ScaleY = newScale;
            ScaleTransform.ScaleX = newScale;

            NeedUpdate = true;
            DebugHelper.ZoomFactor = newScale;
        }
    }
}