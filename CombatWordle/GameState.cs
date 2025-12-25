namespace CombatWordle
{
    public class GameState
    {
        public Map Map { get; private set; }

        public bool GameOver { get; private set; } = true;

        public Player Player { get; private set; }

        public double MapCenterX => Map.Width / 2 - Player.Width / 2;
        public double MapCenterY => Map.Height / 2 - Player.Height / 2;
        public Position MapCenter => new(MapCenterX, MapCenterY);

        public GameState(int mapWidth = 10000, int mapHeight = 10000)
        {
            Map = new(mapWidth, mapHeight);
            AddPlayer();
            GameOver = false;
        }

        private void AddPlayer()
        {
            Player = new(80, 80);
            Player.WorldPos = MapCenter;
        }

        public Rock rock;
        public void AddRock()
        {
            rock = new();
            rock.WorldPos = new(Player.X, Player.Y - 30);
        }

        public void PopulateMap(Entity entity, decimal percentage)
        {

        }
    }
}