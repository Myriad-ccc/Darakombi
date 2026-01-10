namespace Darakombi
{
    public class GameManager : IManager
    {
        public GlobalContext Context { get; set; }
        public GameState Game;
        public Renderer Renderer;

        public GridHelper SpatialCellGrid;
        public const int SpatialCellSize = 128;

        public Player Player => Game?.Player;
        public SpatialGrid SpatialGrid => Game?.SpatialGrid;

        public Rect Viewport => Context.Viewport;
        public Map Map => Context.Map;
        public ScaleTransform ScaleTransform => Context.ScaleTransform;
        public TranslateTransform TranslateTransform => Context.TranslateTransform;
        public HashSet<Key> PressedKeys => Context.PressedKeys;

        public Action<UIElement, int> AddElementToCanvas;
        public Action ClearViewport;
        public Action ClearMap;

        public StringBuilder DynamicDebug { get; set; } = new();
        public StringBuilder EventDebug { get; set; } = new();

        public GameManager(GlobalContext context, Renderer renderer)
        {
            Context = context;
            Renderer = renderer;
        }

        public void Start()
        {
            Game ??= new(Map);
            Game.SpatialGrid ??= new(Map.Width, Map.Height);
            Game.AddPlayer();
            SpatialCellGrid ??= new GridHelper(SpatialCellSize, (int)Map.Width, (int)Map.Height, Brushes.Green, 0.5);
            AddElementToCanvas?.Invoke(SpatialCellGrid, 10);
        }

        public void Move(double dt)
        {
            var d = new Vector();

            if (PressedKeys.Contains(Key.W)) d.Y -= 1;
            if (PressedKeys.Contains(Key.A)) d.X -= 1;
            if (PressedKeys.Contains(Key.S)) d.Y += 1;
            if (PressedKeys.Contains(Key.D)) d.X += 1;

            if (d.X != 0 || d.Y != 0) d.Normalize();
            d.X *= Player.Speed * dt;
            d.Y *= Player.Speed * dt;

            Point pos = Player.Pos;
            Size size = Player.Size;
            Rect newRect;

            double gap = 1e-10;
            double leftEdge = Map.Thickness;
            double topEdge = Map.Thickness;
            double rightEdge = Map.Width - Map.Thickness - Player.Width;
            double bottomEdge = Map.Height - Map.Thickness - Player.Height;

            Rect searchArea = Player.Rect;
            searchArea.Inflate(Player.Speed * dt + 10, Player.Speed * dt + 10);
            var colliders = SpatialGrid.Search(searchArea);

            pos.X += d.X;
            newRect = new Rect(pos, size);
            foreach (var collider in colliders.Where(c => c.Entity.CollisionType != CollisionType.Live))
            {
                if (Player.CollisionType == CollisionType.Ghost) break;
                if (newRect.IntersectsWith(collider.Rect))
                {
                    if (d.X > 0)
                        pos.X = collider.Pos.X - Player.Width - gap;
                    else if (d.X < 0)
                        pos.X = collider.Pos.X + collider.Width + gap;
                    newRect = new Rect(pos, size);
                }
            }

            pos.Y += d.Y;
            newRect = new Rect(pos, size);
            foreach (var collider in colliders.Where(c => c.Entity.CollisionType != CollisionType.Live))
            {
                if (Player.CollisionType == CollisionType.Ghost) break;
                if (newRect.IntersectsWith(collider.Rect))
                {
                    if (d.Y > 0)
                        pos.Y = collider.Pos.Y - Player.Height - gap;
                    else if (d.Y < 0)
                        pos.Y = collider.Pos.Y + collider.Height + gap;
                    newRect = new Rect(pos, size);
                }
            }

            pos.X = Math.Max(leftEdge, Math.Min(pos.X, rightEdge));
            pos.Y = Math.Max(topEdge, Math.Min(pos.Y, bottomEdge));

            if (double.IsNaN(pos.X) || double.IsNaN(pos.Y))
                QOL.WriteOut("Player pos NaN");

            Player.Pos = pos;

            DebugHelper.DeltaX = d.X;
            DebugHelper.DeltaY = d.Y;
            DebugHelper.VelocityX = d.X * 1 / dt;
            DebugHelper.VelocityY = d.Y * 1 / dt;

        }

        public void CameraUpdate()
        {
            double px = Player.Pos.X + Player.Width / 2;
            double py = Player.Pos.Y + Player.Height / 2;

            double screenCenterX = Viewport.Width / 2;
            double screenCenterY = Viewport.Height / 2;

            double offsetX = screenCenterX - px;
            double offsetY = screenCenterY - py;

            offsetX = Math.Min(0, Math.Max(offsetX, Viewport.Width - Game.Map.Width));
            offsetY = Math.Min(0, Math.Max(offsetY, Viewport.Height - Game.Map.Height));

            TranslateTransform.X = offsetX;
            TranslateTransform.Y = offsetY;

            DebugHelper.PlayerX = px;
            DebugHelper.PlayerY = py;
        }

        public void KeyDo()
        {
            if (PressedKeys.Remove(Key.R))
                Game.AddTestRock();
            if (PressedKeys.Remove(Key.G))
                Game.PopulateMap<Rock>(2000);
            if (PressedKeys.Remove(Key.V))
                ClearViewport?.Invoke();
            if (PressedKeys.Remove(Key.M))
                ClearMap?.Invoke();
            if (PressedKeys.Remove(Key.E))
                Game.AddTestEnemy();
        }

        public void Debug()
        {
            //entityCounter.AppendLine("Entities:");
            //entityCounter.AppendLine($"rocks:{game.Rocks.Count}");
            //entityCounter.AppendLine($"enemies:{game.Enemies.Count}");
            //EntityCounter.Text = entityCounter.ToString();
            DynamicDebug.Append(DebugHelper.GetGameDynamic());
            EventDebug.Clear();
            EventDebug.Append(DebugHelper.GetGameEvent());
        }

        public void Update(double dt, Rect viewport)
        {
            Context.Viewport = new(-TranslateTransform.X, -TranslateTransform.Y, viewport.Width, viewport.Height);
            DynamicDebug.Clear();
            //entityCounter.Clear();
            Move(dt);
            CameraUpdate();
            foreach (var entityData in Game.LiveEntities)
                SpatialGrid.Update(entityData);
            Renderer.RenderEntities(SpatialGrid.Search(Viewport));
            KeyDo();
            Debug();
        }

        public void Stop()
        {

        }
    }
}