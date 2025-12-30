namespace CombatWordle
{
    public class GameState
    {
        public Map Map { get; private set; }
        public SpatialGrid spatialGrid;

        public List<Entity> Entities { get; } = [];
        public List<EntityData> AllEntityData { get; } = [];

        public List<EntityData> LiveEntities { get; } = [];

        public List<Player> Players { get; } = [];
        public List<Rock> Rocks { get; set; } = [];

        public Point MapCenter => new(Map.Center.X, Map.Center.Y);

        public GameState(int mapWidth = 12800, int mapHeight = 12800)
        {
            Map = new(mapWidth, mapHeight);
            spatialGrid = new(mapWidth, mapHeight);
            AddPlayer();
        }

        private void AddPlayer()
        {
            var playerMapCenter = new Point(Map.Center.X - 40 / 2, Map.Center.Y - 40 / 2);
            var player = new Player
            {
                Pos = playerMapCenter,
                Width = 80,
                Height = 80
            };
            AddEntity(player);
        }

        public void AddTestRock()
        {
            var rock = new Rock();
            rock.Pos = new Point(Players[0].X, Players[0].Y - Players[0].Height - rock.Height);
            AddEntity(rock, force: false);
        }

        public bool InsideMap(Entity entity) => Map.RectInside(entity.Rect);
        public bool Colliding(Entity entity)
        {
            foreach (var data in spatialGrid.Search(entity.Rect))
                if (data.Entity != entity
                    && data.Entity.CanCollide
                    && data.Rect.IntersectsWith(entity.Rect))
                    return true;
            return false;
        }

        public bool CanSpawn(Entity entity) =>
            InsideMap(entity)
            && !Colliding(entity);

        private Point GetRandomPosition(Entity entity) =>
            new(
                QOL.NextDoubleInRange(Map.Thickness, Map.Width - Map.Thickness - entity.Width),
                QOL.NextDoubleInRange(Map.Thickness, Map.Width - Map.Thickness - entity.Width)
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
                if (!InsideMap(entity)) continue;
                if (force || (!force && !Colliding(entity)))
                    return true;
            }
            while (++spawnAttempts < 100);
            return spawnAttempts < 100;
        }

        private bool AddEntity(Entity entity, bool force = false, bool random = false)
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
            spatialGrid.Add(data);

            if (entity is Player player) Players.Add(player);
            if (entity is Rock rock) Rocks.Add(rock);
        }

        //public void PopulateMap<T>(int count)
        //    where T : Entity, new()
        //{
        //    for (int i = 0; i < count; i++)
        //        AddEntity(new T(), force: false, random: true);
        //}

        public void PopulateMap<T>(int count) 
            where T : Entity, new()
        {
            for (int i = 0; i < count; i++)
                AddEntity(new T(),
                    force: false, random: true);
        }

        public void PopulateMap<T>(decimal percentage, MapSpace type)
        {
            if (type == MapSpace.Available)
            {

            }
        }
    }
}