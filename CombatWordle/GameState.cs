namespace CombatWordle
{
    public class GameState
    {
        public Map Map { get; private set; }

        public bool GameOver { get; private set; } = false;

        public Player Player { get; private set; }

        public List<Entity> Entities { get; } = [];
        public List<EntityData> AllEntityData { get; } = [];

        public HashSet<Entity> Colliders { get; set; } = [];

        public List<Player> Players { get; set; } = [];
        public List<Rock> Rocks { get; set; } = [];

        public Point PlayerMapCenter => new(Map.Center.X - Player.Width / 2, Map.Center.Y - Player.Height / 2);

        public GameState(int mapWidth = 12800, int mapHeight = 12800)
        {
            Map = new(mapWidth, mapHeight);
            AddPlayer();
        }

        private void AddPlayer()
        {
            Player = new(new(), new(80, 80));
            Player.WorldPos = PlayerMapCenter;
            AddEntity(Player);
        }

        public Rock rock;
        public void AddTestRock()
        {
            rock = new(new());
            rock.WorldPos = new(Player.X, Player.Y - Player.Height - rock.Height);
            AddEntity(rock, force: false);
        }

        public bool InsideMap(Entity entity) => Map.RectInside(entity.Rect);
        public bool Colliding(Entity entity) => Colliders.Any(c => c.Rect.IntersectsWith(entity.Rect));

        public bool CanSpawn(Entity entity) =>
            InsideMap(entity)
            && !Colliding(entity);

        private Point GetRandomPosition(Entity entity) => // Random() * (max - min) + min for a random num in range [min..max]
            new(
                Random.Shared.NextDouble() * (Map.Width - Map.Thickness - entity.Width - Map.Thickness) + Map.Thickness,
                Random.Shared.NextDouble() * (Map.Height - Map.Thickness - entity.Height - Map.Thickness) + Map.Thickness
                );

        private void RerollSpawn(Entity entity) =>
            entity.WorldPos = GetRandomPosition(entity);

        public void ClampInsideMap(Entity entity)
        {
            double clampedX = Math.Max(Map.Thickness, Math.Min(entity.X, Map.Width - Map.Thickness - entity.Width));
            double clampedY = Math.Max(Map.Thickness, Math.Min(entity.Y, Map.Height - Map.Thickness - entity.Height));
            entity.WorldPos = new(clampedX, clampedY);
        }

        private bool RandomizeSpawn(Entity entity, bool force = false)
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
                    if (!RandomizeSpawn(entity, force: true))
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
                    if (!RandomizeSpawn(entity, force: false))
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

            EntityData data = new(entity) 
            {
                Visible = false,
                CurrentLoadStage = LoadStage.Registered
            };
            AllEntityData.Add(data);

            if (entity.CanCollide) Colliders.Add(entity);
            if (entity is Player player) Players.Add(player);
            if (entity is Rock rock) Rocks.Add(rock);
        }

        public void PopulateMap<T>(int count)
            where T : Entity, new()
        {
            for (int i = 0; i < count; i++)
                AddEntity(new T(), force: false, random: true);
        }

        public void PopulateMap<T>(decimal percentage, MapSpace type)
        {
            if (type == MapSpace.Available)
            {

            }
        }
    }
}