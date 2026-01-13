namespace Darakombi
{
    public class EditorManager : IManager
    {
        public GlobalContext Context { get; set; }
        public UIElement HUD { get; set; }
        public bool Active { get; set; } = true;

        public Editor Editor;

        public GridHelper EditorGrid;
        private const int EditorCellSize = 64;
        public GridHelper ChunkGrid;
        private const int ChunkSize = 512;

        private bool DraggingCamera;
        private Point LastMousePos;
        private Point LastEOPos;

        private bool CanZoom = false;
        private bool HorizontalScroll = false;

        private bool Drawing = false;

        public Rect Viewport => Context.Viewport;
        public Map Map => Context.Map;
        public ScaleTransform ScaleTransform => Context.ScaleTransform;
        public SkewTransform SkewTransform => Context.SkewTransform;
        public RotateTransform RotateTransform => Context.RotateTransform;
        public TranslateTransform TranslateTransform => Context.TranslateTransform;

        public HashSet<Key> PressedKeys => Context.PressedKeys;

        public event Action<UIElement, int> AddElementToCanvas;
        public event Action<UIElement> RemoveElementFromCanvas;
        public event Action<Size> ResizeMap;

        public StringBuilder DynamicDebug { get; init; } = new();
        public StringBuilder EventDebug { get; init; } = new();
        public StringBuilder StaticDebug { get; init; } = new();
        public StringBuilder ModeDebug { get; set; } = new();

        public EditorManager() { }

        public void Start()
        {
            HUD?.Visibility = Visibility.Visible;
            Editor ??= new(EditorCellSize, ChunkSize);
            Editor.ResizeMap += size => ResizeMap?.Invoke(size);
            EditorGrid ??= new GridHelper(EditorCellSize, (int)Map.Width, (int)Map.Height, Brushes.White, 0.1);
            AddElementToCanvas?.Invoke(Editor, 20);
            AddElementToCanvas?.Invoke(EditorGrid, 10);
            ChunkGrid ??= new GridHelper(ChunkSize, (int)Map.Width, (int)Map.Height, Brushes.White, 0.5);
            AddElementToCanvas?.Invoke(ChunkGrid, 20);
            ResetCam();
        }

        private void Move(double dt)
        {
            double pan = 1000 * dt;
            if (PressedKeys.Contains(Key.W) || PressedKeys.Contains(Key.Up)) TranslateTransform.Y += pan;
            if (PressedKeys.Contains(Key.A) || PressedKeys.Contains(Key.Left)) TranslateTransform.X += pan;
            if (PressedKeys.Contains(Key.D) || PressedKeys.Contains(Key.Right)) TranslateTransform.X -= pan;
            if (PressedKeys.Contains(Key.S) || PressedKeys.Contains(Key.Down)) TranslateTransform.Y -= pan;
            ClampTranslate();
        }

        public void InvokeKeys(double dt)
        {
            Move(dt);
            CanZoom = PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl);  // require [0] later
            HorizontalScroll = PressedKeys.Contains(Key.LeftShift) || PressedKeys.Contains(Key.RightShift);  // require [0] later
        }

        public void DrawTile(Point worldPos, SolidColorBrush color)
        {
            int cellX = (int)Math.Floor(worldPos.X / EditorCellSize) * EditorCellSize;
            int cellY = (int)Math.Floor(worldPos.Y / EditorCellSize) * EditorCellSize;

            var tile = new Editor.Tile(cellX, cellY, new(color.Color.R, color.Color.G, color.Color.B));
            Editor?.Add(tile);

            QOL.D($"Placed block at {cellX}, {cellY}");
        }

        public void ResetCam()
        {
            ScaleTransform.ScaleX = 0.5;
            ScaleTransform.ScaleY = 0.5;
            //DebugManager.ZoomFactor = 0.5;
            TranslateTransform.X = Viewport.Width / 2 - (Map.Center.X * ScaleTransform.ScaleX);
            TranslateTransform.Y = Viewport.Height / 2 - (Map.Center.Y * ScaleTransform.ScaleY);
        }

        public void DebugAdd()
        {
            //DebugManager.MousePosX = WorldMousePos.X;
            //DebugManager.MousePosY = WorldMousePos.Y;
            //EventDebug.Clear();
            //EventDebug.Append(DebugManager.GetEditorEvent());
            //var dyn = DynamicDebug.Length == 0 ? null : DynamicDebug + "\n";
            //var ev = EventDebug.Length == 0 ? null : EventDebug + "\n";
            //var st = StaticDebug.Length == 0 ? null : StaticDebug + "\0";
            //ModeDebug = new StringBuilder($"{dyn}{ev}{st}");
        }

        public void Update(double dt)
        {
            if (Active)
            {
                ModeDebug.Clear();
                if (Editor.BufferTiles.Count > 0)
                    Editor.InvalidateVisual();
                InvokeKeys(dt);
            }
        }

        public void End()
        {
            Active = false;
            HUD?.Visibility = Visibility.Hidden;
            Editor?.Clear();
            Editor = null;
            RemoveElementFromCanvas?.Invoke(EditorGrid);
            EditorGrid = null;
        }

        private Point WorldMousePos;
        private Point ScreenToWorld(Point screenPos) => QOL.ScreenToWorld(screenPos, ScaleTransform, TranslateTransform);
        private void UpdateWorldMousePos(Point screenMousePos) => WorldMousePos = ScreenToWorld(screenMousePos);

        public void OnMouseDown(object sender, MouseButtonEventArgs e, Point screenMousePos, SolidColorBrush color)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                UpdateWorldMousePos(screenMousePos);
                if (Map.RectInside(WorldMousePos))
                {
                    DrawTile(WorldMousePos, color);
                    Drawing = true;
                    LastEOPos = WorldMousePos;
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                QOL.D("Started dragging camera");
                DraggingCamera = true;
                LastMousePos = screenMousePos;
                Mouse.Capture((UIElement)sender);
            }
        }
        public void OnMouseUp(object sender, MouseButtonEventArgs e, Point screenMousePos)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Drawing = false;
                Editor?.CommitBuffer();
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                if (DraggingCamera)
                {
                    QOL.D("Stopped dragging camera");
                    UpdateWorldMousePos(screenMousePos);

                    double dx = screenMousePos.X - LastMousePos.X;
                    double dy = screenMousePos.Y - LastMousePos.Y;

                    TranslateTransform.X += dx;
                    TranslateTransform.Y += dy;

                    LastMousePos = screenMousePos;
                }
                if (Mouse.Captured == sender)
                    Mouse.Capture(null);
            }
        }
        public void OnMouseMove(MouseEventArgs e, Point screenMousePos, SolidColorBrush color)
        {
            UpdateWorldMousePos(screenMousePos);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (DraggingCamera)
                {
                    double dx = screenMousePos.X - LastMousePos.X;
                    double dy = screenMousePos.Y - LastMousePos.Y;

                    TranslateTransform.X += dx;
                    TranslateTransform.Y += dy;

                    LastMousePos = screenMousePos;
                    QOL.D($"Moving camera by {dx},{dy}");
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Drawing && Map.RectInside(WorldMousePos))
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
            }
            ClampTranslate();
        }
        public void OnMouseWheel(MouseWheelEventArgs e, Point screenMousePos)
        {
            double step = 500.0 / ScaleTransform.ScaleX * e.Delta / 120;
            double maxStep = 100;
            step = Math.Max(-maxStep, Math.Min(step, maxStep));

            if ((CanZoom && HorizontalScroll) || (!CanZoom && !HorizontalScroll)) //vert scroll
                TranslateTransform.Y += step;
            else if (CanZoom) //zoom
            {
                UpdateWorldMousePos(screenMousePos);

                double zoom = e.Delta > 0 ? 1.1 : 0.9;
                double newScale = ScaleTransform.ScaleX * zoom;
                newScale = Math.Max(0.125, Math.Min(newScale, 1.25));

                ScaleTransform.ScaleX = ScaleTransform.ScaleY = newScale;
                TranslateTransform.X = screenMousePos.X - WorldMousePos.X * newScale;
                TranslateTransform.Y = screenMousePos.Y - WorldMousePos.Y * newScale;

                //DebugManager.ZoomFactor = newScale;
            }
            else //horz scroll
                TranslateTransform.X += step;
            ClampTranslate();
        }

        private void ClampTranslate()
        {
            TranslateTransform.X =
                -Math.Max(
                    -Map.Width * 0.3175, // in case viewport is larger than max right * (0.3175+1.3175)
                    Math.Min(
                        -TranslateTransform.X / ScaleTransform.ScaleX, //left origin in worldspace
                        Math.Max(
                            -Map.Width * 0.3175, //min left
                            Map.Width * 1.3175 - Viewport.Width //max right
                            )
                        )
                    )
                * ScaleTransform.ScaleX;

            TranslateTransform.Y = -Math.Max(-Map.Height * 0.255,
                Math.Min(-TranslateTransform.Y / ScaleTransform.ScaleY,
                Math.Max(-Map.Height * 0.255, Map.Height * 1.255 - Viewport.Height)))
                * ScaleTransform.ScaleY;
        }
    }
}