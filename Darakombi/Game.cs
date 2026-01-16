namespace Darakombi
{
    public class Game
    {
        public Map Map;
        public SpatialGrid SpatialGrid;

        public List<Entity> Entities { get; } = [];
        public List<EntityData> AllEntityData { get; } = [];

        public List<EntityData> LiveEntities { get; } = [];

        public Player Player;
        public List<Enemy> Enemies { get; } = [];
        public List<Rock> Rocks { get; set; } = [];

        public Point MapCenter => new(Map.Center.X, Map.Center.Y);

        public Game(Map map)
        {
            Map = map;
            SpatialGrid = new(map.Width, map.Height);
        }

        public void AddPlayer()
        {
            Player = new Player(new Size(80, 80));
            Player.Pos = new Point(Map.Center.X - Player.Width / 2, Map.Center.Y - Player.Height / 2);
            AddEntity(Player);
        }

        public void AddTestRock()
        {
            var rock = new Rock();
            rock.Pos = new Point(Player.X, Player.Y - Player.Height - rock.Height);
            AddEntity(rock, force: false);
        }

        public void AddTestEnemy()
        {
            var enemy = new Enemy(new Size(80, 80));
            var x = NextDoubleInRange(Player.X + Player.Width / 2 - Player.Width * 3 - enemy.Width, Player.X + Player.Width * 4 + enemy.Width);
            var y = NextDoubleInRange(Player.Y + Player.Height / 2 - Player.Height * 3 - enemy.Height, Player.Y + Player.Height * 4 + enemy.Height);
            enemy.Pos = new(x, y);
            enemy.CreateOverlay(0.1);
            AddEntity(enemy);
        }

        public bool InsideMap(Rect rect) => Map.RectInside(rect);
        public bool Colliding(Entity entity)
        {
            foreach (var data in SpatialGrid.Search(entity.Rect))
                if (data.Entity != entity
                    && data.Entity.CanCollide
                    && data.Rect.IntersectsWith(entity.Rect))
                    return true;
            return false;
        }

        public bool CanSpawn(Entity entity) =>
            InsideMap(entity.Rect)
            && !Colliding(entity);

        private Point GetRandomPosition(Entity entity) =>
            new(
                NextDoubleInRange(Map.Thickness, Map.Width - Map.Thickness - entity.Width),
                NextDoubleInRange(Map.Thickness, Map.Width - Map.Thickness - entity.Width)
                );

        private void RerollSpawn(Entity entity) =>
            entity.Pos = GetRandomPosition(entity);

        public void ClampInsideMap(Entity entity)
        {
            double clampedX = Math.Max(Map.Thickness, Math.Min(entity.X, Map.Width - Map.Thickness - entity.Width));
            double clampedY = Math.Max(Map.Thickness, Math.Min(entity.Y, Map.Height - Map.Thickness - entity.Height));
            entity.Pos = new(clampedX, clampedY);
        }

        private bool RandomizeSpawnPos(Entity entity, bool force = false)
        {
            int spawnAttempts = 0;
            do
            {
                RerollSpawn(entity);
                if (!InsideMap(entity.Rect)) continue;
                if (force || (!force && !Colliding(entity)))
                    return true;
            }
            while (++spawnAttempts < 100);
            return spawnAttempts < 100;
        }

        private bool AddEntity(Entity entity, bool force = false, bool random = false) //refactor and separate later
        {
            if (force)
            {
                if (random)
                {
                    if (!RandomizeSpawnPos(entity, force: true))
                        return false;
                }
                else
                {
                    ClampInsideMap(entity);
                    if (Colliding(entity))
                        return false;
                }
            }
            else
            {
                if (random)
                {
                    if (!RandomizeSpawnPos(entity, force: false))
                        return false;
                }
                else
                {
                    if (!CanSpawn(entity))
                        return false;
                }
            }
            ClampInsideMap(entity);

            AddToEntities(entity);
            return true;
        }

        private void AddToEntities(Entity entity)
        {
            Entities.Add(entity);

            EntityData data = new(entity);
            AllEntityData.Add(data);
            if (entity.CollisionType == CollisionType.Live) LiveEntities.Add(data);
            SpatialGrid.Add(data);

            if (entity is Enemy enemy) Enemies.Add(enemy);
            if (entity is Rock rock) Rocks.Add(rock);
        }

        public static (double x, double y) NormalizeSpeed(double dx, double dy, double speed, double dt)
        {
            if (dx != 0 || dy != 0)
            {
                double len = Math.Sqrt(dx * dx + dy * dy);
                dx /= len;
                dy /= len;
            }
            return (dx * speed * dt, dy * speed * dt);
        }

        public void EnemyAI(Enemy enemy, double dt)
        {
            double dist = GetDistance(enemy.Center, Player.Center);
            switch (enemy.State)
            {
                case EnemyState.Idle:
                    if (dist < enemy.DetectionRange) enemy.State = EnemyState.Chasing;
                    break;
                case EnemyState.Chasing:
                    if (Player.CollisionType == CollisionType.Ghost)
                    {
                        enemy.State = EnemyState.Idle;
                        return;
                    }

                    double dx = 1 * Math.Sign(enemy.X - Player.X);
                    double dy = 1 * Math.Sign(enemy.Y - Player.Y);
                    (dx, dy) = NormalizeSpeed(dx, dy, enemy.Speed, dt);
                    enemy.Pos = new(enemy.X - dx, enemy.Y - dy);
                    if (dist > enemy.DetectionRange * 1.5) enemy.State = EnemyState.Idle;
                    if (dist < enemy.AgroRange) enemy.State = EnemyState.Attacking;
                    break;
                case EnemyState.Attacking:
                    if (Player.CollisionType == CollisionType.Ghost)
                    {
                        enemy.State = EnemyState.Idle;
                        return;
                    }

                    if (dist > enemy.AgroRange) enemy.State = EnemyState.Chasing;
                    break;
            }
        }

        public void PopulateMap<T>(int count)
            where T : Entity, new()
        {
            for (int i = 0; i < count; i++)
                AddEntity(new T(),
                    force: false, random: true);
        }

        public void ClearEntities()
        {
            Player = null;
            Entities.Clear();
            AllEntityData.Clear();
            LiveEntities.Clear();
            Enemies.Clear();
            Rocks.Clear();
        }
    }
}