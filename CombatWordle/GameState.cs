namespace CombatWordle
{
    public class GameState
    {
        public Map Map { get; private set; }

        public bool GameOver { get; private set; } = false;

        public Player Player { get; private set; }

        public List<Entity> Colliders { get; set; } = [];
        public List<Rock> Rocks { get; set; } = [];

        public Action<Entity> ImplementEntity;

        public Point PlayerMapCenter => new(Map.Center.X - Player.Width / 2, Map.Center.Y - Player.Height / 2);

        public GameState(int mapWidth = 10000, int mapHeight = 10000)
        {
            Map = new(mapWidth, mapHeight);
            AddPlayer();
        }

        private void AddPlayer()
        {
            Player = new(new(), new(80, 80));
            Player.WorldPos = PlayerMapCenter;
        }

        public Rock rock;
        public void AddTestRock()
        {
            rock = new(new());
            rock.WorldPos = new(Player.X, Player.Y - Player.Height - rock.Height);
            AddEntity(rock);
        }

        private void AddEntity(Entity entity)
        {
            if (entity.CanCollide) Colliders.Add(entity);

            if (entity is Rock rock)
                Rocks.Add(rock);
            ImplementEntity?.Invoke(entity);
        }

        private Point GetRandomPosition(Entity entity) =>
            new(
                Random.Shared.NextDouble() * (Map.Width - entity.Width),
                Random.Shared.NextDouble() * (Map.Height - entity.Height)
            );

        public void PopulateMap<T>(int count)
            where T : Entity, new()
        {
            for (int i = 0; i < count; i++)
            {
                var entity = new T();
                entity.WorldPos = GetRandomPosition(entity);
                AddEntity(entity);
            }
        }

        public void PopulateMap(Entity entity, decimal percentage)
        {

        }
    }
}