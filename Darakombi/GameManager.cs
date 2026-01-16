namespace Darakombi
{
    [DebugWatch]
    public class GameManager : IManager
    {
        public GlobalContext Context { get; set; }
        public UIElement HUD { get; set; }
        public bool Active { get; set; } = true;

        public Game Game;
        public Renderer Renderer;

        public GridHelper SpatialCellGrid;
        public const int SpatialCellSize = 128;

        public Player Player => Game?.Player;
        public SpatialGrid SpatialGrid => Game?.SpatialGrid;

        private Rect Viewport => Context.Viewport;
        private Map Map => Context.Map;
        private ScaleTransform ScaleTransform => Context.ScaleTransform;
        private SkewTransform SkewTransform => Context.SkewTransform;
        private RotateTransform RotateTransform => Context.RotateTransform;
        private TranslateTransform TranslateTransform => Context.TranslateTransform;
        private HashSet<Key> PressedKeys => Context.PressedKeys;

        public event Action<UIElement, int> AddElementToCanvas;
        public event Action<UIElement> RemoveElementFromCanvas;
        public event Action ClearViewport;
        public event Action ClearMap;

        public StringBuilder DynamicDebug { get; init; } = new();
        public StringBuilder EventDebug { get; init; } = new();
        public StringBuilder StaticDebug { get; init; } = new();
        public StringBuilder ModeDebug { get; set; } = new();

        public GameManager(Renderer renderer)
        {
            Renderer = renderer;
            DebugManager.Track(this);
        }

        public void Start()
        {
            HUD?.Visibility = Visibility.Visible;
            Game ??= new(Map);
            Game.SpatialGrid ??= new(Map.Width, Map.Height);
            Game.AddPlayer();
            SpatialCellGrid ??= new GridHelper(SpatialCellSize, (int)Map.Width, (int)Map.Height, Brushes.Green, 0.5);
            AddElementToCanvas?.Invoke(SpatialCellGrid, 10);
        }

        public void EntityMove(Player player,double dt)
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
            var colliders = SpatialGrid.Search(searchArea);

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

            if (double.IsNaN(pos.X) || double.IsNaN(pos.Y))
                WriteOut("player pos NaN");

            player.Pos = pos;

            player.VX = d.X * 1 / dt;
            player.VY = d.Y * 1 / dt;
        }

        public void CameraFollow(Entity entity)
        {
            double ex = entity.Pos.X + entity.Width / 2;
            double ey = entity.Pos.Y + entity.Height / 2;

            double screenCenterX = Viewport.Width / 2;
            double screenCenterY = Viewport.Height / 2;

            double offsetX = screenCenterX - ex;
            double offsetY = screenCenterY - ey;

            offsetX = Math.Min(0, Math.Max(offsetX, Viewport.Width - Game.Map.Width));
            offsetY = Math.Min(0, Math.Max(offsetY, Viewport.Height - Game.Map.Height));

            TranslateTransform.X = offsetX;
            TranslateTransform.Y = offsetY;
        }

        public void InvokeKeys()
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

        public void Update(double dt)
        {
            if (Active)
            {
                EntityMove(Game?.Player, dt);
                foreach (var enemy in Game.Enemies) Game.EnemyAI(enemy, dt);
                foreach (var entityData in Game.LiveEntities)
                    SpatialGrid.Update(entityData);
                CameraFollow(Game?.Player);
                Renderer.RenderEntities(SpatialGrid.Search(Viewport));
                InvokeKeys();
            }
        }

        public void End()
        {
            Active = false;
            HUD?.Visibility = Visibility.Collapsed;
            Renderer?.ClearCache();
            Renderer = null;
            RemoveElementFromCanvas?.Invoke(SpatialCellGrid);
            Game?.SpatialGrid = null;
            Game?.ClearEntities();
            Game = null;
        }
    }
}