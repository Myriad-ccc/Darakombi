namespace CombatWordle
{
    public class GameState
    {
        public double MapWidth { get; private set; }
        public double MapHeight { get; private set; }
        public bool GameOver { get; private set; } = false;

        public Player Player { get; private set; }

        private List<Player> players = [];

        public GameState(int mapWidth = 10000, int mapHeight = 10000)
        {
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            AddPlayer();
        }        

        private void AddPlayer()
        {
            Player = new Player();
            Player.WorldPos = new Position(MapWidth / 2 - Player.Width / 2, MapHeight / 2 - Player.Height / 2);
            players.Add(Player);
        }
    }
}